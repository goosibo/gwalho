using System;
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

    static int Dispatch(string[] args)
    {
        string cmd = args[0];

        return cmd switch
        {
            "-h" or "--help" or "help" => Help(args),
            "-v" or "--version"        => Version(),
            "build"                    => Build(args),
            "run"                      => Run(args),
            "new"                      => New(args),
            "bench"                    => Bench(args),
            _                          => LegacyBuild(args)
        };
    }

    // =====================================================
    // build : gwalho build snake.gwl [출력폴더]
    // =====================================================
    static int Build(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("[!]( 사용법: gwalho build <소스파일> [출력폴더] )");
            return 1;
        }

        return CompileToFolder(args[1], args.Length >= 3 ? args[2] : null);
    }

    // =====================================================
    // run : gwalho run snake.gwl [fps]
    // =====================================================
    static int Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("[!]( 사용법: gwalho run <소스파일> [fps] )");
            return 1;
        }

        string sourcePath = args[1];
        int fps = args.Length >= 3 && int.TryParse(args[2], out int f) ? f : 10;
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
    // new : gwalho new snake.gwl
    // =====================================================
    static int New(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("[!]( 사용법: gwalho new <파일경로> )");
            return 1;
        }

        string path = args[1];

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

    // =====================================================
    // bench : gwalho bench [스텝수]
    // =====================================================
    static int Bench(string[] args)
    {
        int steps = args.Length >= 2 && int.TryParse(args[1], out int n) ? n : 10_000_000;

        string bench = """
    [ARRAY](Boot){
        [DONE]
    }

    [ARRAY](Main){
        [DEFN](counter, 0)
        [DEFN](one, 1)
        [DEFN](limit, 100000000)
        [LABL](loop)
        [PLUS](counter, counter, one)
        [LESS](cond, counter, limit)
        [JUMP](jr, cond, loop)
        [DONE]
    }
    """;

        string projectDir = Path.Combine(Path.GetTempPath(), "gwalho_bench_" + Guid.NewGuid().ToString("N"));

        try
        {
            Compiler.Compile(bench, projectDir);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[!]( 벤치마크 소스 컴파일 오류: {ex.Message} )");
            return 1;
        }

        if (!GWVM.Boot(projectDir))
        {
            Console.Error.WriteLine("[!]( VM 부트 실패 )");
            return 1;
        }

        Console.WriteLine($"벤치마크 시작: {steps:N0} 스텝");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        GWVM.Step(steps);
        sw.Stop();

        double seconds = sw.Elapsed.TotalSeconds;
        double opsPerSec = steps / seconds;

        Console.WriteLine($"소요 시간: {seconds:F3}초");
        Console.WriteLine($"처리 속도: {opsPerSec:N0} instructions/sec");
        Console.WriteLine(GWVM.EndRun
            ? "루프가 스텝 수 안에 DONE에 도달함 (더 큰 스텝수로 재측정 권장)"
            : "스텝 상한 도달, 계속 실행 중이었음 (측정 유효)");

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

          gwalho build <소스파일> [출력폴더]
          gwalho run   <소스파일> [fps]
          gwalho new   <파일경로>
          gwalho bench [스텝수]           VM 처리 속도 벤치마크
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