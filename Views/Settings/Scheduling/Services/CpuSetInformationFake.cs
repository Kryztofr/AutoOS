using AutoOS.Helpers.CPU;

namespace AutoOS.Views.Settings.Scheduling.Services
{
    public static class CpuSetInformationFake
    {
        private static List<CpuSet> _fakeCpuSets;

        public static List<CpuSet> FakeCpuSets
        {
            get => _fakeCpuSets;
            set => _fakeCpuSets = value;
        }

        // 12 cores, 24 threads
        public static void Fake5900x()
        {
            var cpuSets = new List<CpuSet>();
            byte lastCoreIndex = 0;
            int count = 24;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i
                };

                if (i % 2 != 0)
                {
                    cpuSet.CoreIndex = lastCoreIndex;
                }
                else
                {
                    cpuSet.CoreIndex = (byte)(i / 2);
                    lastCoreIndex = cpuSet.CoreIndex;
                }

                if (i > 11)
                {
                    cpuSet.LastLevelCacheIndex = 12;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 24 cores (8 P-cores + 16 E-cores), 32 threads
        public static void Fake13900()
        {
            var cpuSets = new List<CpuSet>();
            byte lastCoreIndex = 0;
            int count = 32;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i
                };

                if (i < 16 && i % 2 != 0)
                {
                    cpuSet.CoreIndex = lastCoreIndex;
                }
                else
                {
                    cpuSet.CoreIndex = (byte)(i < 16 ? i / 2 : i - 8);
                    lastCoreIndex = cpuSet.CoreIndex;
                }

                if (i < 16)
                {
                    cpuSet.EfficiencyClass = 1;
                }
                else
                {
                    cpuSet.EfficiencyClass = 0;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 24 cores (8 P-cores + 16 E-cores), 24 threads
        public static void Fake13900WithoutHT()
        {
            var cpuSets = new List<CpuSet>();
            int count = 24;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i,
                    CoreIndex = (byte)i
                };

                if (i < 8)
                {
                    cpuSet.EfficiencyClass = 1;
                }
                else
                {
                    cpuSet.EfficiencyClass = 0;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 8 cores, 8 threads
        public static void Fake8Threads()
        {
            var cpuSets = new List<CpuSet>();
            int count = 8;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i,
                    CoreIndex = (byte)i
                };

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 12 cores
        public static void FakeNumaCCD12Core()
        {
            var cpuSets = new List<CpuSet>();
            int count = 12;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i,
                    CoreIndex = (byte)i
                };

                if (i > 5)
                {
                    cpuSet.LastLevelCacheIndex = 6;
                    cpuSet.NumaNodeIndex = 6;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 12 cores with hyperthreading, 2 CCDs
        public static void Fake2CCD12CoreHT()
        {
            var cpuSets = new List<CpuSet>();
            byte lastCoreIndex = 0;
            int count = 24;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i
                };

                if (i % 2 != 0)
                {
                    cpuSet.CoreIndex = lastCoreIndex;
                }
                else
                {
                    cpuSet.CoreIndex = (byte)(i / 2);
                    lastCoreIndex = cpuSet.CoreIndex;
                }

                if (i > 11)
                {
                    cpuSet.LastLevelCacheIndex = 12;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 14 cores (6 P-cores + 8 E-cores), 20 threads
        public static void Fake13600KF()
        {
            var cpuSets = new List<CpuSet>();
            byte lastCoreIndex = 0;
            int count = 20;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i
                };

                if (i < 12 && i % 2 != 0)
                {
                    cpuSet.CoreIndex = lastCoreIndex;
                }
                else
                {
                    if (i < 12)
                    {
                        cpuSet.CoreIndex = (byte)(i / 2);
                    }
                    else
                    {
                        cpuSet.CoreIndex = (byte)(6 + (i - 12));
                    }
                    lastCoreIndex = cpuSet.CoreIndex;
                }

                if (i < 12)
                {
                    cpuSet.EfficiencyClass = 1;
                }
                else
                {
                    cpuSet.EfficiencyClass = 0;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }
    }
}
