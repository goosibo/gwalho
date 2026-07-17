using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GwalhoCompiler;
using Gwalho;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            RunShell();
            return 0;
        }

        return Dispatch(args);
    }

    // =====================================================
    // 이름 붙은 파라미터 파싱 (-Source snake.gwl -Output ./build 형태)
    // =====================================================
    static Dictionary<string, string> ParseNamedArgs(string[] args, int startIndex)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = startIndex; i < args.Length; i++)
        {
            if (!args[i].StartsWith("-")) continue;

            string key = args[i].TrimStart('-');
            string value = (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                ? args[++i]
                : "true"; // 스위치형 파라미터(-Verbose 같은 것) 대비

            result[key] = value;
        }

        return result;
    }

    static int Dispatch(string[] args)
    {
        string cmd = args[0];

        return cmd switch
        {
            "-h" or "-Help" or "--help" or "help" => Help(args),
            "-v" or "-Version" or "--version" => Version(),
            "build" or "Build-Gwalho" => Build(args),
            "run" or "Invoke-Gwalho" => Run(args),
            "new" or "New-Gwalho" => New(args),
      
            _ => LegacyBuild(args)
        };
    }

    // =====================================================
    // build : gwalho build -Source snake.gwl -Output ./build
    // =====================================================
    static int Build(string[] args)
    {
        var p = ParseNamedArgs(args, 1);

        if (!p.TryGetValue("Source", out string? source))
        {
            Console.Error.WriteLine("[!]( 사용법: gwalho build -Source <파일> [-Output <폴더>] )");
            return 1;
        }

        p.TryGetValue("Output", out string? output);
        return CompileToFolder(source, output);
    }

    // =====================================================
    // run : gwalho run -Source snake.gwl -Fps 10
    // =====================================================
    static int Run(string[] args)
    {
        var p = ParseNamedArgs(args, 1);

        if (!p.TryGetValue("Source", out string? sourcePath))
        {
            Console.Error.WriteLine("[!]( 사용법: gwalho run -Source <파일> [-Fps <숫자>] )");
            return 1;
        }

        int fps = p.TryGetValue("Fps", out string? fpsStr) && int.TryParse(fpsStr, out int f) ? f : 10;
        int frameDelayMs = fps > 0 ? 1000 / fps : 0;

        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"[!]( 파일을 찾을 수 없습니다: {sourcePath} )");
            return 1;
        }

        string projectDir = Path.Combine(Path.GetTempPath(), "gwalho_run_" + Guid.NewGuid().ToString("N"));

        try
        {
            Compiler.Compile(File.ReadAllText(sourcePath), projectDir);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[!]( 컴파일 오류: {ex.Message} )");
            return 1;
        }

        if (!GWVM.Boot(projectDir))
        {
            Console.Error.WriteLine("[!]( VM 부트 실패 (Boot 배열 로드 불가) )");
            return 1;
        }

        const int maxStepsPerFrame = 1_000_000;
        int frame = 0;

        while (true)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                break;

            GWVM.Step(maxStepsPerFrame);

            if (!GWVM.EndRun)
            {
                Console.Error.WriteLine($"[!]( 프레임 {frame}이 스텝 상한 안에 DONE에 도달하지 못함 )");
                return 1;
            }

            frame++;

            int remain = frameDelayMs - (int)sw.ElapsedMilliseconds;
            if (remain > 0)
                System.Threading.Thread.Sleep(remain);
        }

        Console.WriteLine($"[!]( {frame}프레임 실행 후 종료 )");
        return 0;
    }

    // =====================================================
    // new : gwalho new -Path snake.gwl
    // =====================================================
    static int New(string[] args)
    {
        var p = ParseNamedArgs(args, 1);

        if (!p.TryGetValue("Path", out string? path))
        {
            Console.Error.WriteLine("[!]( 사용법: gwalho new -Path <파일경로> )");
            return 1;
        }

        if (File.Exists(path))
        {
            Console.Error.WriteLine($"[!]( 이미 존재하는 파일입니다: {path} )");
            return 1;
        }

        string dir = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(dir);

        File.WriteAllText(path, """
        [ARRAY](Boot){
            [DONE]
        }

        [ARRAY](Main){
            [DONE]
        }
        """);

        Console.WriteLine($"[!]( 생성됨 → {path} )");
        return 0;
    }

    static int LegacyBuild(string[] args) => CompileToFolder(args[0], args.Length >= 2 ? args[1] : null);

    static int CompileToFolder(string sourcePath, string? outputDir)
    {
        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"[!]( 파일을 찾을 수 없습니다: {sourcePath} )");
            return 1;
        }

        outputDir ??= Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sourcePath))!, "build");

        try
        {
            Compiler.Compile(File.ReadAllText(sourcePath), outputDir);
            Console.WriteLine($"[!]( 컴파일 완료 → {outputDir} )");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[!]( 컴파일 오류: {ex.Message} )");
            return 1;
        }
    }

    static int Version()
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        Console.WriteLine($"[!]( gwalho {version} )");
        return 0;
    }

    static int Help(string[] args)
    {
        Console.WriteLine("""
        ================================================================

          gwalho build -Source <파일> [-Output <폴더>]
          gwalho run   -Source <파일> [-Fps <숫자>]
          gwalho new   -Path <파일경로>
          gwalho -Version
        
        ================================================================
        """);
        return 0;
    }

    static void RunShell()
    {
        Console.WriteLine("""

            ****************************************************************
            gwalho Shell.                
            ****************************************************************
                                                                               
            'help'로 명령어 목록보기.                                          
            'exit'로 종료하기.                                                 
                                                                               
            ================================================================
            """);

        while (true)
        {
            Console.Write("gwalho> ");
            string? line = Console.ReadLine();

            if (line == null) break;
            line = line.Trim();
            if (line.Length == 0) continue;

            if (line is "exit" or "quit" or "q")
                break;

            string[] parts = SplitLine(line);
            if (parts.Length == 0) continue;

            try
            {
                Dispatch(parts);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] 오류: {ex.Message}");
            }
        }
    }

    static string[] SplitLine(string line)
    {
        var result = new System.Collections.Generic.List<string>();
        int i = 0;
        while (i < line.Length)
        {
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            if (i >= line.Length) break;

            if (line[i] == '"')
            {
                int end = line.IndexOf('"', i + 1);
                if (end < 0) end = line.Length;
                result.Add(line.Substring(i + 1, end - i - 1));
                i = end + 1;
            }
            else
            {
                int start = i;
                while (i < line.Length && !char.IsWhiteSpace(line[i])) i++;
                result.Add(line.Substring(start, i - start));
            }
        }
        return result.ToArray();
    }

   
}