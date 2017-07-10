﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Transformer.Abstractions.Configuration;
using Hast.Transformer.Vhdl.Abstractions.Configuration;

namespace Hast.Samples.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                using (var hastlayer = await Hastlayer.Create())
                {
                    #region Configuration
                    var configuration = new HardwareGenerationConfiguration("Nexys4 DDR");

                    configuration.AddHardwareEntryPointType<ParallelAlgorithm>();

                    configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                        new MemberInvocationInstanceCountConfigurationForMethod<ParallelAlgorithm>(p => p.Run(null), 0)
                        {
                            MaxDegreeOfParallelism = ParallelAlgorithm.MaxDegreeOfParallelism
                        });

                    configuration.VhdlTransformerConfiguration().VhdlGenerationMode = VhdlGenerationMode.Debug;

                    hastlayer.ExecutedOnHardware += (sender, e) =>
                    {
                        Console.WriteLine(
                            "Executing " +
                            e.MemberFullName +
                            " on hardware took " +
                            e.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds +
                            "ms (net) " +
                            e.HardwareExecutionInformation.FullExecutionTimeMilliseconds +
                            " milliseconds (all together)");
                    };
                    #endregion

                    #region HardwareGeneration
                    var hardwareRepresentation = await hastlayer.GenerateHardware(
                        new[]
                        {
                            typeof(Program).Assembly
                        },
                        configuration);

                    await hardwareRepresentation.HardwareDescription.WriteSource("Hast_IP.vhd");
                    #endregion

                    #region Execution
                    var parallelAlgorithm = await hastlayer.GenerateProxy(hardwareRepresentation, new ParallelAlgorithm());

                    var output1 = parallelAlgorithm.Run(234234);
                    var output2 = parallelAlgorithm.Run(123);
                    var output3 = parallelAlgorithm.Run(9999);

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var cpuOutput = new ParallelAlgorithm().Run(234234);
                    sw.Stop();
                    Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + "ms.");
                    #endregion
                }
            }).Wait();

            Console.ReadKey();
        }
    }
}
