#include <windows.h>
#include <mmdeviceapi.h>
#include <audioclient.h>
#include <string>
#include <vector>
#include <thread>
#include <shellapi.h>
#include <cmath>

using namespace std;

static const PROPERTYKEY PKEY_AudioEngine_DeviceFormat_Local = { {0xf19f064d, 0x082c, 0x4e27, {0xbc, 0x73, 0x68, 0x82, 0xa1, 0xbb, 0x8e, 0x4c}}, 0 };

static float gOutputMs = 0, gInputMs = 0;

static void Relaunch(float out, float in) {
    WCHAR path[MAX_PATH];
    GetModuleFileNameW(NULL, path, MAX_PATH);
    wstring args = L"";
    if (out > 0) args += L"-output-ms " + to_wstring(out) + L" ";
    if (in > 0) args += L"-input-ms " + to_wstring(in) + L" ";
    ShellExecuteW(NULL, L"open", path, args.c_str(), NULL, SW_HIDE);
}

class DeviceNotificationClient : public IMMNotificationClient {
    LONG _ref = 1;
    wstring _outputId, _inputId;

public:
    DeviceNotificationClient(wstring outId, wstring inId) : _outputId(outId), _inputId(inId) {}

    STDMETHOD_(ULONG, AddRef)() { return InterlockedIncrement(&_ref); }
    STDMETHOD_(ULONG, Release)() {
        ULONG ref = InterlockedDecrement(&_ref);
        if (ref == 0) delete this;
        return ref;
    }
    STDMETHOD(QueryInterface)(REFIID riid, void** ppv) {
        if (riid == __uuidof(IUnknown) || riid == __uuidof(IMMNotificationClient)) {
            *ppv = static_cast<IMMNotificationClient*>(this);
            AddRef();
            return S_OK;
        }
        *ppv = NULL;
        return E_NOINTERFACE;
    }

    STDMETHOD(OnDefaultDeviceChanged)(EDataFlow flow, ERole role, LPCWSTR pwstrDeviceId) {
        if (flow == eRender && gOutputMs > 0) {
            Relaunch(gOutputMs, gInputMs);
            TerminateProcess(GetCurrentProcess(), 0);
        }
        else if (flow == eCapture && gInputMs > 0) {
            Relaunch(gOutputMs, gInputMs);
            TerminateProcess(GetCurrentProcess(), 0);
        }
        return S_OK;
    }

    STDMETHOD(OnPropertyValueChanged)(LPCWSTR pwstrDeviceId, const PROPERTYKEY key) {
        if (pwstrDeviceId && (key.fmtid == PKEY_AudioEngine_DeviceFormat_Local.fmtid && key.pid == PKEY_AudioEngine_DeviceFormat_Local.pid)) {
            wstring id = pwstrDeviceId;
            if ((gOutputMs > 0 && id == _outputId) || (gInputMs > 0 && id == _inputId)) {
                Relaunch(gOutputMs, gInputMs);
                TerminateProcess(GetCurrentProcess(), 0);
            }
        }
        return S_OK;
    }

    STDMETHOD(OnDeviceStateChanged)(LPCWSTR pwstrDeviceId, DWORD dwNewState) { return S_OK; }
    STDMETHOD(OnDeviceAdded)(LPCWSTR pwstrDeviceId) { return S_OK; }
    STDMETHOD(OnDeviceRemoved)(LPCWSTR pwstrDeviceId) { return S_OK; }
};

static void HandleEndpoint(EDataFlow flow, float ms) {
    if (ms <= 0) return;
    (void)CoInitializeEx(NULL, COINIT_MULTITHREADED);
    {
        IMMDeviceEnumerator* pEnumerator = NULL;
        if (SUCCEEDED(CoCreateInstance(__uuidof(MMDeviceEnumerator), NULL, CLSCTX_ALL, __uuidof(IMMDeviceEnumerator), (void**)&pEnumerator))) {
            IMMDevice* pDevice = NULL;
            if (SUCCEEDED(pEnumerator->GetDefaultAudioEndpoint(flow, eConsole, &pDevice))) {
                IAudioClient3* pAudioClient = NULL;
                if (SUCCEEDED(pDevice->Activate(__uuidof(IAudioClient3), CLSCTX_ALL, NULL, (void**)&pAudioClient))) {
                    WAVEFORMATEX* pFormat = NULL;
                    if (SUCCEEDED(pAudioClient->GetMixFormat(&pFormat))) {
                        UINT32 frames = (UINT32)lround((double)ms * pFormat->nSamplesPerSec / 1000.0);
                        UINT32 def = 0, fund = 0, minP = 0, maxP = 0;
                        if (SUCCEEDED(pAudioClient->GetSharedModeEnginePeriod(pFormat, &def, &fund, &minP, &maxP))) {
                            if (frames < minP) {
                                if (flow == eRender) Relaunch(0, gInputMs);
                                else Relaunch(gOutputMs, 0);
                                TerminateProcess(GetCurrentProcess(), 0);
                            }
                            if (frames > maxP) frames = maxP;
                            if (fund > 0 && frames % fund != 0) {
                                frames = (frames / fund) * fund;
                                if (frames < minP) frames = minP;
                            }
                        }
                        if (SUCCEEDED(pAudioClient->InitializeSharedAudioStream(0, frames, pFormat, NULL))) {
                            pAudioClient->Start();
                            while (true) Sleep(INFINITE);
                        }
                        CoTaskMemFree(pFormat);
                    }
                    pAudioClient->Release();
                }
                pDevice->Release();
            }
            pEnumerator->Release();
        }
    }
    CoUninitialize();
}

int APIENTRY wWinMain(_In_ HINSTANCE hInstance, _In_opt_ HINSTANCE hPrevInstance, _In_ LPWSTR lpCmdLine, _In_ int nCmdShow) {
    MSG msg;
    PeekMessage(&msg, NULL, 0, 0, PM_REMOVE);
    (void)SetPriorityClass(GetCurrentProcess(), IDLE_PRIORITY_CLASS);
    
    PROCESS_POWER_THROTTLING_STATE throttling = { 0 };
    throttling.Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION;
    throttling.ControlMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED;
    throttling.StateMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED;
    (void)SetProcessInformation(GetCurrentProcess(), ProcessPowerThrottling, &throttling, sizeof(throttling));

    int argc;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (!argv || argc < 2) { if (argv) LocalFree(argv); return 0; }

    for (int i = 1; i < argc; i++) {
        wstring arg = argv[i];
        if ((arg == L"-output-ms" || arg == L"-output") && i + 1 < argc) {
            wstring val = argv[++i];
            for (size_t j = 0; j < val.length(); ++j) if (val[j] == L',') val[j] = L'.';
            gOutputMs = (float)_wtof(val.c_str());
        }
        else if ((arg == L"-input-ms" || arg == L"-input") && i + 1 < argc) {
            wstring val = argv[++i];
            for (size_t j = 0; j < val.length(); ++j) if (val[j] == L',') val[j] = L'.';
            gInputMs = (float)_wtof(val.c_str());
        }
    }

    (void)CoInitializeEx(NULL, COINIT_MULTITHREADED);
    IMMDeviceEnumerator* pEnumerator = NULL;
    DeviceNotificationClient* pClient = NULL;
    wstring outId, inId;

    if (SUCCEEDED(CoCreateInstance(__uuidof(MMDeviceEnumerator), NULL, CLSCTX_ALL, __uuidof(IMMDeviceEnumerator), (void**)&pEnumerator))) {
        if (gOutputMs > 0) {
            IMMDevice* pD = NULL;
            if (SUCCEEDED(pEnumerator->GetDefaultAudioEndpoint(eRender, eConsole, &pD))) {
                PWSTR sid = NULL; pD->GetId(&sid); outId = sid; CoTaskMemFree(sid); pD->Release();
            }
        }
        if (gInputMs > 0) {
            IMMDevice* pD = NULL;
            if (SUCCEEDED(pEnumerator->GetDefaultAudioEndpoint(eCapture, eConsole, &pD))) {
                PWSTR sid = NULL; pD->GetId(&sid); inId = sid; CoTaskMemFree(sid); pD->Release();
            }
        }
        pClient = new DeviceNotificationClient(outId, inId);
        pEnumerator->RegisterEndpointNotificationCallback(pClient);
    }

    vector<thread> threads;
    if (gOutputMs > 0) threads.emplace_back(HandleEndpoint, eRender, gOutputMs);
    if (gInputMs > 0) threads.emplace_back(HandleEndpoint, eCapture, gInputMs);

    while (GetMessage(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    if (pEnumerator) {
        if (pClient) { pEnumerator->UnregisterEndpointNotificationCallback(pClient); pClient->Release(); }
        pEnumerator->Release();
    }
    CoUninitialize();
    if (argv) LocalFree(argv);
    return 0;
}
