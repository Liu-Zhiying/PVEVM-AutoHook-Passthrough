using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
    static readonly string vmStatusDir = "/var/run/qemu-server/";
    static readonly string driDir = "/sys/bus/pci/drivers/";
    static readonly string vfio_pciDir = "/sys/bus/pci/drivers/vfio-pci";
    static readonly string snapshotFile = "./dev_snapshot";
    static readonly string vmDevPassthroughFile = "/var/run/qemu-server/pci-id-reservations";
    static readonly string passthroughDriName = "vfio-pci";

    [DllImport("c", EntryPoint = "system", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private extern static int system(string cmd);

    static bool NameLikeDevice(string f)
    {
        return f.Length == 12 && f[4] == ':' && f[7] == ':' && f[10] == '.';
    }

    static void ShowHelp()
    {
        Console.WriteLine("Please add \"--make_snapshot\" to make snapshot");
        Console.WriteLine("or add \"--auto_check\" to auto check device do not bind after vm closing!");
    }

    static void Main(string[] args)
    {
        Console.WriteLine("This program is only designed for PVE,Please do not using in other OS.");
        if (args.Length < 1)
        {
            ShowHelp();
            return;
        }
        else
        {
            switch (args[0])
            {
                case "--make_snapshot":
                    MakeSnapshot();
                    break;
                case "--auto_check":
                    AutoCheck();
                    break;
                default:
                    ShowHelp();
                    break;
            }
        }
    }



    static void MakeSnapshot()
    {
        StreamWriter? writer = null;

        AppDomain.CurrentDomain.UnhandledException += (o, e) =>
        {
            writer?.Close();
            writer = null;
            Console.WriteLine("Make snapshot error!");
        };
        if (!Directory.Exists(vfio_pciDir)
            || !Directory.GetDirectories(vfio_pciDir).Any(d => NameLikeDevice(Path.GetFileName(d))))
        {
            writer = new StreamWriter(new FileStream(snapshotFile, FileMode.Create));
            foreach (string driDir in Directory.GetDirectories(driDir))
            {
                foreach (string devFile in Directory.GetDirectories(driDir))
                {
                    string devName = Path.GetFileName(devFile);
                    string driName = Path.GetFileName(driDir);
                    //设备文件格式举例： 0000:07:00.1
                    if (NameLikeDevice(devName))
                    {
                        writer.WriteLine($"{devName}={driName}");
                        Console.WriteLine($"{devName}={driName}");
                    }
                }
            }
            writer.Close();
            writer = null;
        }
        else
        {
            Console.WriteLine("Please make snapshot without passthrough!");
        }
    }

    static void AutoCheck()
    {
        StreamReader? fileReader = null;
        Dictionary<string, string> devsMap = new();
        AppDomain.CurrentDomain.UnhandledException += (o, e) =>
        {
            fileReader?.Close();
            fileReader = null;
            Console.WriteLine("Occur exception when checking!");
        };
        if (File.Exists(snapshotFile))
        {
            fileReader = new StreamReader(new FileStream(snapshotFile, FileMode.Open));
            while (!fileReader.EndOfStream)
            {
                string[]? temp = fileReader.ReadLine()?.Split("=");
                if (temp != null && temp.Length == 2)
                {
                    devsMap.Add(temp[0], temp[1]);
                }
            }
            fileReader.Close();
            fileReader = null;
        }
        else
        {
            Console.WriteLine("Please make snapshot first!");
            return;
        }
        while (true)
        {
            List<string> devsPassthrough = new();
            List<string> devsBind = new();
            if (File.Exists(vmDevPassthroughFile))
            {
                fileReader = new StreamReader(new FileStream(vmDevPassthroughFile, FileMode.Open));
                while (!fileReader.EndOfStream)
                {
                    string[]? temp = fileReader.ReadLine()?.Split(" ");
                    if (temp != null && temp.Length >= 2
                        && File.Exists(Path.Combine(vmStatusDir, $"{temp[1]}.pid")))
                    {
                        devsPassthrough.Add(temp[0]);
                    }
                }
                fileReader.Close();
                fileReader = null;
            }

            foreach (string singleDriDir in Directory.GetDirectories(driDir))
            {
                foreach (string devFile in Directory.GetDirectories(singleDriDir))
                {
                    string devName = Path.GetFileName(devFile);
                    string driName = Path.GetFileName(singleDriDir);

                    //设备文件格式举例： 0000:07:00.1
                    if (NameLikeDevice(devName) && driName != passthroughDriName)
                    {
                        devsBind.Add(devName);
                    }
                }
            }

            IEnumerable<string> resultArray = devsMap.Keys.Where(dev => !devsPassthrough
                .Contains(dev) && !devsBind.Contains(dev));

            //Console.WriteLine($"Scan time: {DateTime.Now}");

            //Console.WriteLine("Bind devices:");
            //foreach (string bindDev in devsBind)
            //    Console.WriteLine(bindDev);

            //Console.WriteLine("Passthourgh devices");
            //foreach (string passthroughDev in devsPassthrough)
            //    Console.WriteLine(passthroughDev);
            //ProcessStartInfo startParam = new()
            //{
            //    FileName = "/bin/echo"
            //};

            foreach (string result in resultArray)
            {
                if (devsMap.TryGetValue(result, out string? value))
                {
                    //startParam.Arguments = $"{result} > {Path.Combine(vfio_pciDir, "unbind")}";
                    //Process? process = Process.Start(startParam);
                    //process?.WaitForExit();
                    //process?.Close();
                    _ = system($"echo {result} > {Path.Combine(vfio_pciDir, "unbind")}");
                    Console.WriteLine($"echo {result} > {Path.Combine(vfio_pciDir, "unbind")} executed!");

                    //startParam.Arguments = $" {result} > {Path.Combine(driDir, value, "bind")}";
                    //process = Process.Start(startParam);
                    //process?.WaitForExit();
                    //process?.Close();
                    _ = system($"echo {result} > {Path.Combine(driDir, value, "bind")}");
                    Console.WriteLine($"echo {result} > {Path.Combine(driDir, value, "bind")} executed!");
                }
            }
            Thread.Sleep(5000);
        }
    }
}
