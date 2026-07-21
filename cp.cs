using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text; 
namespace GwalhoCompiler
{
    // =====================================================
    // VM TYPES (BXVM과 1:1로 맞춰야 합니다)
    // =====================================================

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct METADATA
    {
        public const uint MAGIC = 0x5B39355D;
        public uint Magic;
        public int ID;
        public int Length;
        public int Base;
        public byte Exists;
    }

   
    public enum OP : int
    {
        NOPE,
        DEFN,
        MOVE,
        PLUS,
        MNUS,
        MULT,
        DIVD,
        MODL,
        BAND,
        BORR,
        BXOR,
        BNOT,
        BNOR,
        BNND,
        BXNR,

        BITR,
        BITW,

        BFLR,
        BFLW,

        BSHL,
        BROL,
        BSHR,
        BROR,
        BUSR,
        GRET,
        LESS,
        EQUL,
        NEQL,
        GEQL,
        LEQL,
        JUMP,
        READ,
        WRTE,
        ARGW,
        ARGR,
        RETW, RETR,
        CALL,
        EXIT,
        COPY,
        ALOC,
        DELT,
        COMP,
        FREE,
        LOAD,
        SAVE,
        FILL,
        SUMM,
        AVRG,
        FIND,
        SORT,
        MINM,
        MAXM,
        CONT,
        RESZ,
        LNTH,
        EXST,
        BASE,
        SWAP,
        DONE,
        RNDM,




        EXTRA = NOPE
    }

    public enum ArgKind
    {
        Register,
        Label,
        Immediate
    }

    public sealed class Instruction
    {
        public OP Op;
        public int A;
        public int B;
        public int C;
    }

    public sealed class ArrayBlock
    {
        public string Name;
        public int ID;

        public List<string> Lines = new();
        public Dictionary<string, int> Symbols = new();
        public Dictionary<string, int> Labels = new();
    }

    public sealed class Project
    {
        public List<ArrayBlock> ArrayBlocks = new();
        public Dictionary<string, int> ArrayBlockIDs = new();
        public Dictionary<string, int> Constants = new();
    }

    public static class Compiler
    {
        // =====================================================
        // ENTRY
        // =====================================================

        static string Normalize(string source)
        {
            while (true)
            {
                int start = source.IndexOf("/*");
                if (start < 0) break;

                int end = source.IndexOf("*/", start + 2);
                if (end < 0)
                    throw new Exception("Unclosed block comment");

                source = source.Remove(start, end - start + 2);
            }

            var lines = source.Split('\n');
            var stripped = new StringBuilder();

            foreach (var line in lines)
            {
                int idx = line.IndexOf("//");
                stripped.Append(idx >= 0 ? line.Substring(0, idx) : line);
                stripped.Append('\n');
            }

            source = stripped.ToString();

            return string.Concat(source.Where(c => !char.IsWhiteSpace(c)));
        }

        public static Project Parse(string source)
        {
            source = Normalize(source);

            List<string> raw = ExtractCommands(source);

            HashSet<string> ArrayBlockNames = CollectArrayBlockNames(raw);

            Dictionary<string, int> constants = new();
            List<string> commands = new();

            foreach (string line in raw)
            {
                if (line.StartsWith("[$]("))
                {
                    string[] parts = SplitArgs(Between(line));

                    if (parts.Length == 2)
                    {
                        string name = parts[0].Trim();

                        if (!int.TryParse(parts[1].Trim(), out int value))
                            throw new Exception($"Invalid Global Constant : {line}");

                        if (ArrayBlockNames.Contains(name))
                            throw new Exception($"Name Conflict Between Constant And Array : '{name}'");

                        if (constants.ContainsKey(name))
                            throw new Exception($"Duplicate Global Constant : {name}");

                        constants[name] = value;
                        continue;
                    }
                }

                commands.Add(line);
            }

            Project project = ParseArrayBlocks(commands);
            project.Constants = constants;

            BuildArrayBlockIDs(project);

            foreach (var ArrayBlock in project.ArrayBlocks)
            {
                if (IsLogic(ArrayBlock))
                {
                    ExpandLiterals(ArrayBlock);
                    BuildLabels(ArrayBlock);
                    BuildSymbols(ArrayBlock);
                    ValidateLogicArrayBlock(ArrayBlock);
                }
            }

            return project;
        }

        // =====================================================
        // COMMAND EXTRACTION ( '[' '(' '<' 중첩을 안전하게 지원 )
        // =====================================================

        static List<string> ExtractCommands(string source)
        {
            List<string> raw = new();
            int i = 0;
            int n = source.Length;

            while (i < n)
            {
                if (source[i] != '[')
                {
                    i++;
                    continue;
                }

                int start = i;

                int nameEnd = source.IndexOf(']', i);
                if (nameEnd < 0)
                    throw new Exception("Unclosed '[' In Source");

                i = nameEnd + 1;

                if (i < n && source[i] == '(')
                {
                    int depth = 0;
                    int j = i;

                    while (j < n)
                    {
                        if (source[j] == '(') depth++;
                        else if (source[j] == ')')
                        {
                            depth--;
                            if (depth == 0) { j++; break; }
                        }
                        j++;
                    }

                    if (depth != 0)
                        throw new Exception("Unclosed '(' In Source");

                    i = j;
                }

                // 배열 헤더용 트레일링 '{'
                if (i < n && source[i] == '{')
                    i++;

                // 리터럴 라인용 트레일링 '<반복수>'
                if (i < n && source[i] == '<')
                {
                    int close = source.IndexOf('>', i);
                    if (close < 0)
                        throw new Exception("Unclosed '<' In Source");
                    i = close + 1;
                }

                string line = source.Substring(start, i - start).Trim();

                if (!string.IsNullOrEmpty(line))
                    raw.Add(line);
            }

            return raw;
        }

        static HashSet<string> CollectArrayBlockNames(List<string> raw)
        {
            HashSet<string> names = new();

            foreach (string line in raw)
            {
                if (!line.StartsWith("[ARRAY](") || !line.EndsWith("{"))
                    continue;

                List<string> groups = Groups(line);
                if (groups.Count < 1) continue;

                string name = groups[0].Trim();
                if (name.Length > 0)
                    names.Add(name);
            }

            return names;
        }

        static List<string> Groups(string line)
        {
            List<string> result = new();
            int i = 0;

            while (i < line.Length)
            {
                if (line[i] != '(')
                {
                    i++;
                    continue;
                }

                int start = ++i;
                int depth = 1;

                while (i < line.Length && depth > 0)
                {
                    if (line[i] == '(') depth++;
                    else if (line[i] == ')') depth--;
                    i++;
                }

                result.Add(line.Substring(start, i - start - 1));
            }

            return result;
        }

        // =====================================================
        // LITERAL / $이름 인라인 지원 (레지스터 자리)
        // =====================================================

        static void ExpandLiterals(ArrayBlock ArrayBlock)
        {
            List<string> output = new();
            int temp = 0;

            foreach (string line in ArrayBlock.Lines)
            {
                if (!line.StartsWith("["))
                {
                    output.Add(line);
                    continue;
                }

                if (TryParseLiteral(line, out _, out _, out _))
                {
                    output.Add(line); // 데이터 리터럴 라인은 손대지 않음
                    continue;
                }

                string op = GetOp(line);

                if (op == "LABL")
                {
                    output.Add(line);
                    continue;
                }

                string[] args = GetArgs(line);

                if (args.Length == 0)
                {
                    output.Add(line);
                    continue;
                }

                Sig.TryGetValue(op, out var sig);
                Slot[] slots = sig?.Slots;
                string[] newArgs = (string[])args.Clone();
                bool changed = false;

                for (int i = 0; i < args.Length; i++)
                {
                    Slot? slot = (slots != null && i < slots.Length) ? slots[i] : (Slot?)null;
                    ArgKind kind = slot?.Kind ?? ArgKind.Register;
                    if (kind != ArgKind.Register) continue;

                    string a = args[i].Trim();

                    bool isLiteral = IsLiteralToken(a);
                    bool isDollarRef = a.StartsWith("$");

                    if (!isLiteral && !isDollarRef) continue;

                    if (slot != null && slot.Value.Dest)
                        throw new Exception(
                            $"Numbers or '$' cannot be used in the result section; only register or symbol names are permitted. : {line}");

                    string tempName = $"__L{temp++}";
                    output.Add($"[DEFN]({tempName},{a})");
                    newArgs[i] = tempName;
                    changed = true;
                }

                output.Add(changed ? $"[{op}]({string.Join(",", newArgs)})" : line);
            }

            ArrayBlock.Lines = output;
        }

        static int ResolveDollarName(Project project, string name, string line)
        {
            if (project.Constants.TryGetValue(name, out int cval))
                return cval;

            if (project.ArrayBlockIDs.TryGetValue(name, out int sid))
                return sid;

            throw new Exception($"Unknown Constant Or ArrayBlock : {name} (Line: {line})");
        }

        static int ResolveDataValue(Project project, string token, string line)
        {
            token = token.Trim();

            if (token.StartsWith("$"))
                return ResolveDollarName(project, token.Substring(1), line);

            if (!int.TryParse(token, out int value))
                throw new Exception($"Invalid Data Value : '{token}' (Line: {line})");

            return value;
        }

        static void ValidateLogicArrayBlock(ArrayBlock ArrayBlock)
        {
            string lastOp = null;

            foreach (string line in ArrayBlock.Lines)
            {
                if (!line.StartsWith("["))
                    continue;

                if (TryParseLiteral(line, out _, out _, out _))
                    continue; // 리터럴은 흐름 판단 대상 아님

                string op = GetOp(line);

                if (op != "LABL")
                    lastOp = op;
            }

            if (lastOp != "DONE" && lastOp != "EXIT")
                throw new Exception($"Array '{ArrayBlock.Name}' must end with [DONE] or [EXIT].");
        }

        // =====================================================
        // ArrayBlockS
        // =====================================================

        static Project ParseArrayBlocks(List<string> commands)
        {
            Project project = new();
            ArrayBlock current = null;

            foreach (string line in commands)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("[ARRAY](") && line.EndsWith("{"))
                {
                    List<string> groups = Groups(line);

                    if (groups.Count < 1)
                        throw new Exception($"Invalid Array Header : {line}");

                    string name = groups[0].Trim();
                    if (name.Length == 0)
                        throw new Exception($"Invalid Array Header (Empty Name) : {line}");

                    current = new ArrayBlock { Name = name };
                    project.ArrayBlocks.Add(current);
                    continue;
                }

                if (line == "}")
                {
                    current = null;
                    continue;
                }

                current?.Lines.Add(line);
            }

            return project;
        }

        static void BuildArrayBlockIDs(Project project)
        {
            ArrayBlock main = project.ArrayBlocks.FirstOrDefault(x => x.Name == "Main");
            ArrayBlock boot = project.ArrayBlocks.FirstOrDefault(x => x.Name == "Boot");

            if (main == null)
                throw new Exception("Main not found");
            if (boot == null)
                throw new Exception("Boot not found");

            boot.ID = 0;
            project.ArrayBlockIDs["Boot"] = 0;
            main.ID = 1;
            project.ArrayBlockIDs["Main"] = 1;

            int next = 2;

            foreach (var ArrayBlock in project.ArrayBlocks)
            {
                if (ArrayBlock == main) continue;
                if (ArrayBlock == boot) continue;

                ArrayBlock.ID = next;
                project.ArrayBlockIDs[ArrayBlock.Name] = next;
                next++;
            }
        }

        // =====================================================
        // 배열이 Logic처럼 동작하는지 판별
        // (실제 명령어 라인이 하나라도 있으면 Logic, 전부 리터럴/라벨뿐이면 Data처럼 동작)
        // =====================================================

        static bool IsLogic(ArrayBlock block) =>
            block.Lines.Any(line =>
                line.StartsWith("[") &&
                !line.StartsWith("[LABL](") &&
                !TryParseLiteral(line, out _, out _, out _));

        // =====================================================
        // 리터럴 데이터 라인 판별 : [OFFSET](v,v,...)<REPEAT>
        // OFFSET/REPEAT는 비어있거나 정수/$이름. opcode로 등록된 헤더는 제외.
        // =====================================================

        static bool TryParseLiteral(string line, out string offset, out string[] values, out string repeat)
        {
            offset = null; values = Array.Empty<string>(); repeat = null;

            string head = GetOp(line);
            if (Sig.ContainsKey(head) || head == "LABL" || head == "ARRAY" || head == "$")
                return false;

            offset = head.Trim();
            values = GetArgs(line);

            int lt = line.LastIndexOf('<');
            if (lt >= 0 && line.EndsWith(">"))
                repeat = line.Substring(lt + 1, line.Length - lt - 2).Trim();

            return true;
        }

        static bool IsLiteralToken(string raw)
        {
            raw = raw.Trim();
            if (raw.Length == 0) return false;
            return int.TryParse(raw, out _);
        }

        static int SlotsForLine(string line)
        {
            string op = GetOp(line);

            if (!Sig.TryGetValue(op, out var sig))
                throw new Exception($"Unknown Or Not Explicitly Implemented OP : {op} (Line: {line})");

            return sig.InstrCount;
        }

        static void BuildLabels(ArrayBlock ArrayBlock)
        {
            ArrayBlock.Labels.Clear();
            int pc = 0;

            foreach (string line in ArrayBlock.Lines)
            {
                if (line.StartsWith("[LABL](")) { ArrayBlock.Labels[Between(line)] = pc; continue; }
                if (!line.StartsWith("[")) continue;
                if (TryParseLiteral(line, out _, out _, out _)) continue; // 리터럴은 PC 미기여

                pc += SlotsForLine(line);
            }
        }

        // =====================================================
        // SYMBOLS
        // =====================================================

        static void CollectSymbol(ArrayBlock ArrayBlock, string raw, ref int next)
        {
            raw = raw.Trim();
            if (raw.Length == 0) return;
            if (IsLiteralToken(raw)) return;
            if (ArrayBlock.Symbols.ContainsKey(raw)) return;

            ArrayBlock.Symbols[raw] = next++;
        }

        static ArgKind[] GetArgKinds(string op) =>
            Sig.TryGetValue(op, out var sig) ? sig.Kinds : null;

        static void BuildSymbols(ArrayBlock ArrayBlock)
        {
            ArrayBlock.Symbols.Clear();
            int next = 0;

            foreach (string line in ArrayBlock.Lines)
            {
                if (!line.StartsWith("[")) continue;
                if (TryParseLiteral(line, out _, out _, out _)) continue;

                string op = GetOp(line);
                if (op == "LABL") continue;

                string[] args = GetArgs(line);
                ArgKind[] kinds = GetArgKinds(op);

                for (int i = 0; i < args.Length; i++)
                {
                    ArgKind kind = (kinds != null && i < kinds.Length) ? kinds[i] : ArgKind.Register;
                    if (kind != ArgKind.Register) continue;

                    CollectSymbol(ArrayBlock, args[i], ref next);
                }
            }
        }

        // =====================================================
        // RESOLVE
        // =====================================================

        static int ResolveRegister(ArrayBlock ArrayBlock, string value)
        {
            value = value.Trim();

            if (!ArrayBlock.Symbols.TryGetValue(value, out int reg))
                throw new Exception($"Unknown Symbol : {value}");

            return reg;
        }

        static int ResolveLabel(ArrayBlock ArrayBlock, string value)
        {
            if (!ArrayBlock.Labels.TryGetValue(value, out int pc))
                throw new Exception($"Unknown Label : {value}");

            return pc;
        }

        static int ResolveImmediate(Project project, string value)
        {
            value = value.Trim();

            if (value.StartsWith("$"))
                return ResolveDollarName(project, value.Substring(1), value);

            if (!int.TryParse(value, out int result))
                throw new Exception($"Invalid Immediate Value : {value}");

            return result;
        }

        // =====================================================
        // INSTRUCTION SIGNATURES
        // =====================================================

        readonly struct Slot
        {
            public readonly ArgKind Kind;
            public readonly int Instr;
            public readonly char Field;
            public readonly bool Dest;
            public Slot(ArgKind kind, int instr, char field, bool dest = false)
            { Kind = kind; Instr = instr; Field = field; Dest = dest; }
        }

        sealed class OpSig
        {
            public readonly OP Op;
            public readonly Slot[] Slots;
            public ArgKind[] Kinds => Array.ConvertAll(Slots, s => s.Kind);
            public int InstrCount => Slots.Length == 0 ? 1 : Slots.Max(s => s.Instr) + 1;

            public OpSig(OP op, params Slot[] slots) { Op = op; Slots = slots; }
        }

        static Slot R(char f) => new(ArgKind.Register, 0, f);
        static Slot L(char f) => new(ArgKind.Label, 0, f);
        static Slot I(char f) => new(ArgKind.Immediate, 0, f);
        static Slot R2(char f) => new(ArgKind.Register, 1, f);

        static readonly Dictionary<string, OpSig> Sig = new()
        {
            ["DEFN"] = new(OP.DEFN, R('A'), I('B')),
            ["MOVE"] = new(OP.MOVE, R('A'), R('B')),

            ["PLUS"] = new(OP.PLUS, R('A'), R('B'), R('C')),
            ["MNUS"] = new(OP.MNUS, R('A'), R('B'), R('C')),
            ["MULT"] = new(OP.MULT, R('A'), R('B'), R('C')),
            ["DIVD"] = new(OP.DIVD, R('A'), R('B'), R('C')),
            ["MODL"] = new(OP.MODL, R('A'), R('B'), R('C')),

            ["GRET"] = new(OP.GRET, R('A'), R('B'), R('C')),
            ["LESS"] = new(OP.LESS, R('A'), R('B'), R('C')),
            ["GEQL"] = new(OP.GEQL, R('A'), R('B'), R('C')),
            ["LEQL"] = new(OP.LEQL, R('A'), R('B'), R('C')),
            ["EQUL"] = new(OP.EQUL, R('A'), R('B'), R('C')),
            ["NEQL"] = new(OP.NEQL, R('A'), R('B'), R('C')),

            ["BNOT"] = new(OP.BNOT, R('A'), R('B')),
            ["BAND"] = new(OP.BAND, R('A'), R('B'), R('C')),
            ["BORR"] = new(OP.BORR, R('A'), R('B'), R('C')),
            ["BXOR"] = new(OP.BXOR, R('A'), R('B'), R('C')),
            ["BNND"] = new(OP.BNND, R('A'), R('B'), R('C')),
            ["BNOR"] = new(OP.BNOR, R('A'), R('B'), R('C')),
            ["BXNR"] = new(OP.BXNR, R('A'), R('B'), R('C')),
            ["BITR"] = new(OP.BITR, R('A'), R('B'), R('C')),
            ["BITW"] = new(OP.BITW, R('A'), R('B'), R('C'), R2('A')),
            ["BFLR"] = new(OP.BFLR, R('A'), R('B'), R('C'), R2('A')),
            ["BFLW"] = new(OP.BFLW, R('A'), R('B'), R('C'), R2('A'), R2('B')),

            ["BSHL"] = new(OP.BSHL, R('A'), R('B'), R('C')),
            ["BROR"] = new(OP.BROR, R('A'), R('B'), R('C')),
            ["BROL"] = new(OP.BROL, R('A'), R('B'), R('C')),
            ["BUSR"] = new(OP.BUSR, R('A'), R('B'), R('C')),
            ["BSHR"] = new(OP.BSHR, R('A'), R('B'), R('C')),

            ["JUMP"] = new(OP.JUMP, R('A'), R('B'), L('C')),
            ["LABL"] = new(OP.NOPE),
            ["DONE"] = new(OP.DONE),

            ["ARGW"] = new(OP.ARGW, R('A'), R('B'), R('C')),
            ["RETR"] = new(OP.RETR, R('A'), R('B')),
            ["ARGR"] = new(OP.ARGR, R('A'), R('B')),
            ["RETW"] = new(OP.RETW, R('A'), R('B'), R('C')),
            ["CALL"] = new(OP.CALL, R('A'), R('B')),
            ["EXIT"] = new(OP.EXIT),

            ["READ"] = new(OP.READ, R('A'), R('B'), R('C')),
            ["WRTE"] = new(OP.WRTE, R('A'), R('B'), R('C'), R2('A')),

            ["LOAD"] = new(OP.LOAD, R('A'), R('B')),
            ["SAVE"] = new(OP.SAVE, R('A'), R('B')),
            ["ALOC"] = new(OP.ALOC, R('A'), R('B')),
            ["FREE"] = new(OP.FREE, R('A'), R('B')),

            ["RESZ"] = new(OP.RESZ, R('A'), R('B'), R('C')),
            ["SWAP"] = new(OP.SWAP, R('A'), R('B'), R('C'), R2('A'), R2('B')),
            ["COMP"] = new(OP.COMP, R('A')),
            ["DELE"] = new(OP.DELT, R('A'), R('B')),

            ["LNTH"] = new(OP.LNTH, R('A'), R('B')),
            ["EXST"] = new(OP.EXST, R('A'), R('B')),
            ["BASE"] = new(OP.BASE, R('A'), R('B')),

            ["COPY"] = new(OP.COPY, R('A'), R('B'), R('C'), R2('A'), R2('B'), R2('C')),

            ["SUMM"] = new(OP.SUMM, R('A'), R('B'), R('C'), R2('A')),
            ["AVRG"] = new(OP.AVRG, R('A'), R('B'), R('C'), R2('A')),
            ["MINM"] = new(OP.MINM, R('A'), R('B'), R('C'), R2('A')),
            ["MAXM"] = new(OP.MAXM, R('A'), R('B'), R('C'), R2('A')),
            ["CONT"] = new(OP.CONT, R('A'), R('B'), R('C'), R2('A'), R2('B')),
            ["FILL"] = new(OP.FILL, R('A'), R('B'), R('C'), R2('A'), R2('B')),
            ["FIND"] = new(OP.FIND, R('A'), R('B'), R('C'), R2('A'), R2('B')),
            ["SORT"] = new(OP.SORT, R('A'), R('B'), R('C'), R2('A'), R2('B')),
            ["RNDM"] = new(OP.RNDM, R('A'), R('B'), R('C')),
        };

        // =====================================================
        // EMIT (Logic/Data 통합 워드 방출)
        // =====================================================

        static List<Instruction> EmitInstruction(Project project, ArrayBlock ArrayBlock, string line)
        {
            string op = GetOp(line);
            string[] args = GetArgs(line);

            if (op == "LABL")
                return new() { new Instruction { Op = OP.NOPE, A = 0, B = 0, C = 0 } };

            if (!Sig.TryGetValue(op, out var sig))
                throw new Exception($"Unknown Or Not Explicitly Implemented OP : {op} (Sig 테이블에 등록되지 않음)");

            if (args.Length != sig.Slots.Length)
                throw new Exception($"'{op}' Expects {sig.Slots.Length} Arg(s), Got {args.Length} : {line}");

            int instrCount = sig.InstrCount;
            int[] a = new int[instrCount];
            int[] b = new int[instrCount];
            int[] c = new int[instrCount];

            for (int i = 0; i < sig.Slots.Length; i++)
            {
                Slot slot = sig.Slots[i];

                int value = slot.Kind switch
                {
                    ArgKind.Register => ResolveRegister(ArrayBlock, args[i]),
                    ArgKind.Label => ResolveLabel(ArrayBlock, args[i]),
                    ArgKind.Immediate => ResolveImmediate(project, args[i]),
                    _ => throw new Exception("Unknown ArgKind")
                };

                switch (slot.Field)
                {
                    case 'A': a[slot.Instr] = value; break;
                    case 'B': b[slot.Instr] = value; break;
                    case 'C': c[slot.Instr] = value; break;
                }
            }

            var instructions = new List<Instruction>(instrCount);

            for (int i = 0; i < instrCount; i++)
            {
                instructions.Add(new Instruction
                {
                    Op = (i == 0) ? sig.Op : OP.EXTRA,
                    A = a[i],
                    B = b[i],
                    C = c[i]
                });
            }

            return instructions;
        }

        // 라인을 순서대로 읽어 하나의 워드 버퍼에 씀.
        // 리터럴 라인([OFFSET](v,v..)<REPEAT>)은 어디서든 raw 워드를 그대로 꽂고,
        // 그 외 라인(실제 명령어)은 이 배열이 Logic으로 판별될 때만 허용됨.
        static int[] EmitWords(Project project, ArrayBlock block)
        {
            bool isLogic = IsLogic(block);
            var words = new List<int>();
            int cursor = 0;
            void Ensure(int idx) { while (words.Count <= idx) words.Add(0); }

            foreach (string line in block.Lines)
            {
                if (!line.StartsWith("[")) continue;
                if (line.StartsWith("[LABL](")) continue;

                if (TryParseLiteral(line, out string offTok, out string[] valTok, out string repTok))
                {
                    int offset = string.IsNullOrEmpty(offTok) ? cursor : ResolveDataValue(project, offTok, line);
                    int repeat = string.IsNullOrEmpty(repTok) ? 1 : ResolveDataValue(project, repTok, line);
                    int[] vals = valTok.Select(v => ResolveDataValue(project, v, line)).ToArray();

                    int pos = offset;
                    for (int r = 0; r < repeat; r++)
                        foreach (int v in vals) { Ensure(pos); words[pos++] = v; }

                    cursor = pos;
                }
                else
                {
                    if (!isLogic)
                        throw new Exception($"Non-literal instruction '{line}' inside a data-only array '{block.Name}'");

                    foreach (var ins in EmitInstruction(project, block, line))
                    {
                        Ensure(cursor + 3);
                        words[cursor] = (int)ins.Op; words[cursor + 1] = ins.A;
                        words[cursor + 2] = ins.B; words[cursor + 3] = ins.C;
                        cursor += 4;
                    }
                }
            }

            return words.ToArray();
        }

        static string Between(string line)
        {
            int p0 = line.IndexOf('(');
            int p1 = line.LastIndexOf(')');
            return line.Substring(p0 + 1, p1 - p0 - 1);
        }

        static string GetOp(string line)
        {
            int p = line.IndexOf(']');
            return line.Substring(1, p - 1);
        }

        static string[] SplitArgs(string body)
        {
            if (string.IsNullOrEmpty(body)) return Array.Empty<string>();

            var result = new List<string>();
            int depth = 0, start = 0;

            for (int i = 0; i < body.Length; i++)
            {
                char ch = body[i];

                if (ch == '(') depth++;
                else if (ch == ')') depth--;
                else if (ch == ',' && depth == 0)
                {
                    result.Add(body.Substring(start, i - start));
                    start = i + 1;
                }
            }

            result.Add(body.Substring(start));
            return result.ToArray();
        }

        static string[] GetArgs(string line)
        {
            int p0 = line.IndexOf('(');
            if (p0 < 0) return Array.Empty<string>();

            string body = Between(line);
            if (string.IsNullOrEmpty(body)) return Array.Empty<string>();

            return SplitArgs(body);
        }

        static METADATA CreateHeader(ArrayBlock ArrayBlock, int length)
        {
            return new METADATA
            {
                Magic = METADATA.MAGIC,
                ID = ArrayBlock.ID,
                Length = length,
                Base = 0,
                Exists = 1,
            };
        }

        static unsafe void WriteHeader(BinaryWriter bw, METADATA header)
        {
            bw.Write(new ReadOnlySpan<byte>(&header, sizeof(METADATA)));
        }

        static void WriteArrayBlock(string folder, ArrayBlock ArrayBlock, int[] data)
        {
            METADATA header = CreateHeader(ArrayBlock, data.Length);
            string path = Path.Combine(folder, $"{ArrayBlock.ID}.gwl");

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var fs = File.Create(path);
            using var bw = new BinaryWriter(fs);

            unsafe { WriteHeader(bw, header); }

            foreach (int value in data)
                bw.Write(value);
        }

        public static void Build(Project project, string projectRoot)
        {
            Directory.CreateDirectory(projectRoot);

            foreach (var ArrayBlock in project.ArrayBlocks)
            {
                int[] code = EmitWords(project, ArrayBlock);
                WriteArrayBlock(projectRoot, ArrayBlock, code);
            }
        }

        public static void Compile(string source, string projectRoot)
        {
            Project project = Parse(source);
            Build(project, projectRoot);
        }
    }
}
