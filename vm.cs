
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Gwalho
{
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

        SHLE,
        ROLE,
        SHRI,
        RORI,
        USHR,
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
    public enum VMState
    {
        Running,
        Compacting,
        DONE,

    }
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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct FRAME
    {
        public int PrevArrayBlock;
        public int PrevPC;
        public int Self;
        public fixed int Registers[1024];
        public fixed int ARGS[1024];
        public fixed int RTNS[1024];
    }
    public static unsafe class GWVM
    {



        public static string RootPath { get; private set; }

        public static string ProjectPath { get; private set; }

        public static bool EndRun = false;

        public const int MEMORY_SIZE = 1 << 23;
        public const int MAX_ArrayBlock = 1 << 24;
        public const int MAX_FRAME = 1024;

        public const int Regi_COUNT = 1024;
        public const int Args_COUNT = 1024;
        public const int rtns_COUNT = 1024;

        public static int[] MEMORY = new int[MEMORY_SIZE];
        public static int[] Compaction_MEMORY = new int[MEMORY_SIZE];
        public static METADATA[] Metadatas = new METADATA[MAX_ArrayBlock];
        public static int[] used_ID = new int[MAX_ArrayBlock];
        public static int used_count = 0;
        public static FRAME[] Frames = new FRAME[MAX_FRAME];
        public static int FrameTop = 0;
        public static VMState State = VMState.DONE;
        public static int CurrentArrayBlock = 0;
        public static int PC = 0;
        public static int HeapTop = 1;

        static readonly System.Random _rng = new System.Random(); // SORT mode=2(무작위) 셔플용

        public static delegate*<int*, FRAME*, void>[] Ops;
        static GWVM()
        {
            Ops = new delegate*<int*, FRAME*, void>[256];
            Ops[(int)OP.NOPE] = &NOPE;
            Ops[(int)OP.DEFN] = &DEFN;
            Ops[(int)OP.MOVE] = &MOVE;
            Ops[(int)OP.PLUS] = &PLUS;
            Ops[(int)OP.MNUS] = &MNUS;
            Ops[(int)OP.MULT] = &MULT;
            Ops[(int)OP.DIVD] = &DIVD;
            Ops[(int)OP.MODL] = &MODL;

            Ops[(int)OP.BAND] = &BAND;
            Ops[(int)OP.BORR] = &BORR;
            Ops[(int)OP.BXOR] = &BXOR;
            Ops[(int)OP.BNOT] = &BNOT;
            Ops[(int)OP.BNND] = &BNND;
            Ops[(int)OP.BXNR] = &BXNR;
            Ops[(int)OP.BNOR] = &BNOR;

            Ops[(int)OP.BITR] = &BITR;
            Ops[(int)OP.BITW] = &BITW;

            Ops[(int)OP.BFLR] = &BFLR;
            Ops[(int)OP.BFLW] = &BFLW;


            Ops[(int)OP.SHLE] = &SHLE;
            Ops[(int)OP.ROLE] = &ROLE;
            Ops[(int)OP.RORI] = &RORI;
            Ops[(int)OP.SHRI] = &SHRI;
            Ops[(int)OP.USHR] = &USHR;

            Ops[(int)OP.GRET] = &GRET;
            Ops[(int)OP.LESS] = &LESS;
            Ops[(int)OP.EQUL] = &EQUL;
            Ops[(int)OP.NEQL] = &NEQL;
            Ops[(int)OP.GEQL] = &GEQL;
            Ops[(int)OP.LEQL] = &LEQL;

            Ops[(int)OP.JUMP] = &JUMP;
            Ops[(int)OP.READ] = &READ;
            Ops[(int)OP.WRTE] = &WRTE;
            Ops[(int)OP.ARGW] = &ARGW;
            Ops[(int)OP.ARGR] = &ARGR;
            Ops[(int)OP.RETW] = &RETW;
            Ops[(int)OP.RETR] = &RETR;
            Ops[(int)OP.CALL] = &CALL;
            Ops[(int)OP.EXIT] = &EXIT;

            Ops[(int)OP.COPY] = &COPY;
            Ops[(int)OP.ALOC] = &ALOC;
            Ops[(int)OP.FREE] = &FREE;
            Ops[(int)OP.LOAD] = &LOAD;
            Ops[(int)OP.SAVE] = &SAVE;
            Ops[(int)OP.FILL] = &FILL;
            Ops[(int)OP.FIND] = &FIND;

            Ops[(int)OP.RESZ] = &RESZ;
            Ops[(int)OP.SWAP] = &SWAP;


            Ops[(int)OP.SUMM] = &SUMM;
            Ops[(int)OP.AVRG] = &AVRG;
            Ops[(int)OP.MINM] = &MINM;
            Ops[(int)OP.MAXM] = &MAXM;
            Ops[(int)OP.SORT] = &SORT;
            Ops[(int)OP.CONT] = &CONT;

            Ops[(int)OP.LNTH] = &LNTH;

            Ops[(int)OP.EXST] = &EXST;

            Ops[(int)OP.BASE] = &BASE;

            Ops[(int)OP.RNDM] = &RNDM;
            Ops[(int)OP.DONE] = &DONE;


        }


        [MethodImpl(MethodImplOptions.NoInlining)]


        // ================== ID 유효성 검사 (공통) ==================
        private static bool IsValidID(int id)
        {
            return (uint)id < MAX_ArrayBlock;
        }

        // ================== 등록 (내부 전용, Alloc/Load가 공유) ==================
        // 예전 CreateArrayBlock의 로직입니다. 외부에서 직접 호출할 이유가 없어서
        // private 헬퍼로 내렸습니다. (다른 곳에서 CreateArrayBlock을 직접 부르고
        // 있었다면 그 호출부는 RegisterBlock으로 바꾸거나 Allocate/Load로 대체해야 해요)
        //   1) 메타데이터배열확인 -> 2) 아이디시도 -> (파일 접근은 여기서 안 함)
        private static bool RegisterBlock(METADATA meta)
        {
            int id = meta.ID;

            if (!IsValidID(id))
                return false;

            if (meta.Length <= 0)
                return false;

            if (meta.Length > MEMORY_SIZE - HeapTop)
            {
                Compact();

                if (meta.Length > MEMORY_SIZE - HeapTop)
                    return false;
            }

            meta.Base = HeapTop;
            meta.Exists = 1;

            Metadatas[id] = meta;
            used_ID[used_count++] = id;

            HeapTop += meta.Length;

            return true;
        }

        static void RemoveUsedID(int id)
        {
            int* used = (int*)Unsafe.AsPointer(ref used_ID[0]);

            for (int i = 0; i < used_count; i++)
            {
                if (used[i] == id)
                {
                    used[i] = used[--used_count];
                    return;
                }
            }
        } // 사용중인 아이디에서 제거합니다.

        // ================== Alloc ==================
        public static int AllocateArrayBlock(int length)
        {
            for (int i = 0; i < MAX_ArrayBlock; i++)
            {
                if (Metadatas[i].Exists == 0)
                {
                    METADATA meta = new METADATA
                    {
                        Magic = METADATA.MAGIC,
                        ID = i,
                        Length = length,
                        Base = 0,
                        Exists = 0,
                    };

                    return RegisterBlock(meta) ? i : 0;
                }
            }

            return 0;
        } // 빈 슬롯을 찾아 새 배열을 등록합니다. 실패하면 0을 반환합니다.

        // ================== Free ==================
        public static bool FreeArrayBlock(int id)
        {

            if (!IsValidID(id))
                return false;

            if (Metadatas[id].Exists == 0)
                return false;


            if (id == CurrentArrayBlock)
                return false;

            for (int i = 0; i <= FrameTop; i++)
                if (Frames[i].Self == id)
                    return false;

            Metadatas[id].Exists = 0;
            RemoveUsedID(id);

            return true;
        } // 배열을 메모리에서 지웁니다.

        // ================== Save ==================
        public static bool SaveArrayBlock(int id)
        {
            // 1) 메타데이터배열확인
            if (!IsValidID(id))
                return false;

            var h = Metadatas[id];

            if (h.Exists == 0)
                return false;

            // 2) 아이디시도 (경로 준비)
            string path = GetPath(id);
            string dir = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // 3) 실제파일헤더 접근
            using var bw = new BinaryWriter(File.Open(path, FileMode.Create));

            bw.Write(new ReadOnlySpan<byte>(&h, sizeof(METADATA)));

            int* mem = (int*)Unsafe.AsPointer(ref MEMORY[0]);
            int ptr = h.Base;

            for (int i = 0; i < h.Length; i++)
                bw.Write(mem[ptr + i]);

            return true;
        } // 배열을 파일로 씁니다.

        // ================== Load ==================
        public static bool LoadArrayBlock(int id)
        {
            // 1) 메타데이터배열확인
            if (!IsValidID(id))
                return false;

            // 2) 아이디시도 (경로만 확보, 아직 파일 내용은 안 읽음)
            string path = GetPath(id);

            if (!File.Exists(path))
                return false;

            try
            {
                // 3) 실제파일헤더 접근
                using var br = new BinaryReader(File.OpenRead(path));

                byte[] headerBytes = br.ReadBytes(sizeof(METADATA));
                METADATA diskHeader;

                fixed (byte* p = headerBytes)
                {
                    diskHeader = *(METADATA*)p;
                }

                if (diskHeader.ID != id)
                    return false;

                if (diskHeader.Length <= 0)
                    return false;

                if (diskHeader.Length > MEMORY_SIZE)
                    return false;

                if (diskHeader.Magic != METADATA.MAGIC)
                    return false;

                if (!RegisterBlock(diskHeader))
                    return false;

                int baseAddr = Metadatas[id].Base;
                int* mem = (int*)Unsafe.AsPointer(ref MEMORY[0]);

                for (int i = 0; i < diskHeader.Length; i++)
                    mem[baseAddr + i] = br.ReadInt32();

                var runtimeHeader = diskHeader;
                runtimeHeader.Base = baseAddr;
                runtimeHeader.Exists = 1;

                Metadatas[id] = runtimeHeader;

                return true;
            }
            catch
            {
                return false;
            }
        } // 헤더를 읽어 메타데이터를 등록하고 배열을 메모리에 올립니다.

        // ================== 압축 ==================
        public static void Compact()
        {
            State = VMState.Compacting;
            int newTop = 1;

            Array.Clear(Compaction_MEMORY, 0, Compaction_MEMORY.Length);

            for (int i = 0; i < used_count; i++)
            {
                int id = used_ID[i];

                if (Metadatas[id].Exists == 0)
                    continue;

                var h = Metadatas[id];

                Array.Copy(MEMORY, h.Base, Compaction_MEMORY, newTop, h.Length);

                h.Base = newTop;
                Metadatas[id] = h;

                newTop += h.Length;
            }

            (MEMORY, Compaction_MEMORY) = (Compaction_MEMORY, MEMORY);

            HeapTop = newTop;
            State = VMState.Running;
        }

        // ================== 경로 ==================
        public static string GetPath(int id)
        {
            return GetArrayBlockFile(id);
        }

        public static string GetProjectFile(string name)
        {
            return Path.Combine(ProjectPath, name);
        }

        public static string GetArrayBlockFile(int id)
        {
            return Path.Combine(ProjectPath, $"{id}.gwl");
        }

        // ================== 부팅 ==================
        public static bool Boot(string projectPath)
        {
            Frames[0] = default;
            Frames[0].Self = 0;
            ProjectPath = Path.GetFullPath(projectPath);

            if (!Directory.Exists(ProjectPath))
                Directory.CreateDirectory(ProjectPath);

            HeapTop = 1;
            used_count = 0;

            Array.Clear(Metadatas, 0, Metadatas.Length);
            Array.Clear(MEMORY, 0, MEMORY.Length);
            Array.Clear(Frames, 0, Frames.Length);

            CurrentArrayBlock = 0;
            PC = 0;
            FrameTop = 0;

            if (!LoadArrayBlock(0))
            {

                return false;
            }

            State = VMState.Running;
            return true;
        }

        public static void Step(int count)
        {
            State = VMState.Running;


            for (int i = 0; i < count; i++)
            {

                dip();
                if (EndRun)
                    break;
            }

        }
        public static void dip()
        {



            fixed (int* mem = MEMORY)
            fixed (FRAME* frames = Frames)
            {

                if (State != VMState.Running) return;

                if (EndRun)   // 직전 호출이 [DONE]으로 끝났으면 새 프레임 시작
                {
                    FrameTop = 0;
                    CurrentArrayBlock = 1;
                    PC = 0;
                    EndRun = false;
                }

                FRAME* frame = frames + FrameTop;

                var h = Metadatas[CurrentArrayBlock];

                int instructionCount = h.Length >> 2;

                if ((uint)PC >= (uint)instructionCount)
                {

                    return;
                }

                int* ip = mem + h.Base + (PC << 2);

                int op = ip[0];

                if ((uint)op >= (uint)Ops.Length || Ops[op] == null)
                    return;



                PC++;

                Ops[op](ip, frame);

            }
        }

        static void NOPE(int* ip, FRAME* frame)
        { }
        static void DEFN(int* ip, FRAME* frame)
        {
            frame->Registers[ip[1]] =
                ip[2];
        }
        static void MOVE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]];
        }
        static void PLUS(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] +
                reg[ip[3]];

        }
        static void MNUS(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] -
                reg[ip[3]];



        }
        static void MULT(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] *
                reg[ip[3]];



        }
        static void DIVD(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            // [수정 5] 0으로 나누면 DivideByZeroException이 그대로 터져
            // try/catch까지 올라가 "Run Exception" 크래시를 유발하므로 방어.
            if (reg[ip[3]] == 0)
            {
                reg[ip[1]] = 0;
                return;
            }

            reg[ip[1]] =
                reg[ip[2]] /
                reg[ip[3]];



        }
        static void MODL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            // [수정 6] DIV와 동일한 이유로 0 나누기 방어
            if (reg[ip[3]] == 0)
            {
                reg[ip[1]] = 0;
                return;
            }

            reg[ip[1]] =
                reg[ip[2]] %
                reg[ip[3]];



        }
        static void BAND(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] &
                reg[ip[3]];



        }
        static void BORR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] |
                reg[ip[3]];



        }
        static void BXOR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] ^
                reg[ip[3]];


        }
        static void BNOT(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            reg[ip[1]] =
            reg[ip[2]];



        }
        static void BNND(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                ~(reg[ip[2]] &
                  reg[ip[3]]);

            //($"NAND : R[{ip[1]}]({reg[ip[1]]}) = ~( R[{ip[2]}]({reg[ip[1]]}) & R[{ip[3]}]({reg[ip[1]]}) )");
        }
        static void BNOR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                ~(reg[ip[2]] |
                  reg[ip[3]]);
            //($"NOR : R[{ip[1]}]({reg[ip[1]]}) = ~( R[{ip[2]}]({reg[ip[1]]}) | R[{ip[3]}]({reg[ip[1]]}) )");
        }
        static void BXNR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                ~(reg[ip[2]] ^
                  reg[ip[3]]);
            //($"XNOR : R[{ip[1]}]({reg[ip[1]]}) = ~( R[{ip[2]}]({reg[ip[1]]}) ^ R[{ip[3]}]({reg[ip[1]]}) )");
        }
        static void BITR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                (reg[ip[2]] >> reg[ip[3]]) & 1;

            //($"BITR : R[{ip[1]}]({reg[ip[1]]}) = ( R[{ip[2]}]({reg[ip[1]]}) >> R[{ip[3]}]({reg[ip[1]]}) ) & 1 ");
        }
        static void BITW(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int value = reg[ip[2]];
            int idx = reg[ip[3]] & 31;
            int bit = reg[ip[5]]; // EXTRA 슬롯의 A

            if (bit != 0)
            {
                reg[ip[1]] = value | (1 << idx);
                //($"BITW : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) | (1 << (R[{ip[3]}]({reg[ip[1]]})  & 31)) ");

            }
            else
            {
                reg[ip[1]] = value & ~(1 << idx);
                //($"BITW : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) & ~ (1 << (R[{ip[3]}]({reg[ip[1]]})  & 31)) ");

            }
        }
        static void BFLR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int value = reg[ip[2]];
            int offset = reg[ip[3]];
            int width = reg[ip[5]];

            uint mask = width >= 32 ? 0xFFFFFFFF : (1u << width) - 1;
            reg[ip[1]] = (int)(((uint)value >> offset) & mask);


        }
        static void BFLW(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int value = reg[ip[2]];
            int insertVal = reg[ip[6]];
            int offset = reg[ip[3]];
            int width = reg[ip[5]];

            uint mask = width >= 32 ? 0xFFFFFFFF : (1u << width) - 1;
            uint cleared = (uint)value & ~(mask << offset);
            uint inserted = ((uint)insertVal & mask) << offset;

            reg[ip[1]] = (int)(cleared | inserted);

            //($"BFLW : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]})[(R[{ip[3]}]({reg[ip[1]]})+...+R[{ip[5]}])({reg[ip[1]]})] = R[{ip[6]}]({reg[ip[1]]}))");
        }
        static void SHLE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]]
                << (reg[ip[3]] & 31);

            //($" Shift(L) : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) << R[{ip[3]}]({reg[ip[1]]})");
        }
        static void ROLE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int value = reg[ip[2]];
            int shift = reg[ip[3]] & 31;
            reg[ip[1]] = (value << shift) | (int)((uint)value >> (32 - shift));

            //($" Rotate(L) : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) <@ R[{ip[3]}]({reg[ip[1]]})");
        }
        static void RORI(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int value = reg[ip[2]];
            int shift = reg[ip[3]] & 31;
            reg[ip[1]] = (value >> shift) | (int)((uint)value << (32 - shift));


            //($" Rotate(R) : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) @> R[{ip[3]}]({reg[ip[1]]})");
        }
        static void SHRI(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]]
                >> (reg[ip[3]] & 31);

            //($" Shift(R) : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) >> R[{ip[3]}]({reg[ip[1]]})");

        }
        static void USHR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                (int)(
                    (uint)reg[ip[2]]
                    >> (reg[ip[3]] & 31));

            //($" C-Shift(R) : R[{ip[1]}]({reg[ip[1]]}) = R[{ip[2]}]({reg[ip[1]]}) >>> R[{ip[3]}]({reg[ip[1]]})");

        }
        static void GRET(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] >
                reg[ip[3]]
                ? 1 : 0;

            //($" Great : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) > R[{ip[3]}]({reg[ip[1]]}))? 1:0");

        }
        static void LESS(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] <
                reg[ip[3]]
                ? 1 : 0;
            //($" Less : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) < R[{ip[3]}]({reg[ip[1]]}))? 1:0");

        }
        static void EQUL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] ==
                reg[ip[3]]
                ? 1 : 0;
            //($" Equal : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) == R[{ip[3]}]({reg[ip[1]]}))? 1:0");

        }
        static void NEQL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] !=
                reg[ip[3]]
                ? 1 : 0;

            //($" Not_Equal : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) != R[{ip[3]}]({reg[ip[1]]}))");

        }
        static void GEQL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] >=
                reg[ip[3]]
                ? 1 : 0;

            //($" Great_or_Equal : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) >= R[{ip[3]}]({reg[ip[1]]}))? 1:0");

        }
        static void LEQL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] =
                reg[ip[2]] <=
                reg[ip[3]]
                ? 1 : 0;

            //($" Less_or_Equal : R[{ip[1]}]({reg[ip[1]]}) = (R[{ip[2]}]({reg[ip[1]]}) <= R[{ip[3]}]({reg[ip[1]]}))? 1:0");

        }
        static void JUMP(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            reg[ip[1]] = 0;

            if (reg[ip[2]] != 0)
            {
                PC = ip[3];
                reg[ip[1]] = 1;
            }

        }
        static void READ(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int ArrayBlockId = reg[ip[2]];


            reg[ip[1]] = 0;

            if ((uint)ArrayBlockId >= MAX_ArrayBlock || Metadatas[ArrayBlockId].Exists == 0)
                return;

            var h = Metadatas[ArrayBlockId];
            int addr = reg[ip[3]];

            if ((uint)addr >= (uint)h.Length)
                return;

            reg[ip[1]] = MEMORY[h.Base + addr];

        }
        static void WRTE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int ArrayBlockid = reg[ip[2]];
            int addr = reg[ip[3]];
            int value = reg[ip[5]]; // EXTRA 슬롯의 A

            reg[ip[1]] = 0;

            if ((uint)ArrayBlockid >= MAX_ArrayBlock || Metadatas[ArrayBlockid].Exists == 0)
                return;

            var h = Metadatas[ArrayBlockid];


            if ((uint)addr >= (uint)h.Length)
                return;



            int oldValue = MEMORY[h.Base + addr];
            MEMORY[h.Base + addr] = value;

            if (oldValue != value)

                reg[ip[1]] = 1;
        }
        static void ARGW(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int idx = frame->Registers[ip[2]];

            if ((uint)idx >= Args_COUNT)
            {
                reg[ip[1]] = 0;
                return;
            }

            frame->ARGS[idx] =
                frame->Registers[ip[3]];



        }
        static void ARGR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int idx = frame->Registers[ip[2]];

            if ((uint)idx >= Args_COUNT)
            {

                return;
            }

            frame->Registers[ip[1]] =
                frame->ARGS[idx];


        }
        static void RETW(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int idx = frame->Registers[ip[2]];
            reg[ip[1]] = 1;
            if ((uint)idx >= rtns_COUNT)
            {

                return;
            }

            frame->RTNS[idx] =
                frame->Registers[ip[3]];
            reg[ip[1]] = 0;
            //($"[RETW] : RETURNS[{ip[1]}]({frame->RTNS[idx]}) = R[{ip[2]}]({reg[ip[1]]})");
        }
        static void RETR(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int idx = frame->Registers[ip[2]];

            if ((uint)idx >= rtns_COUNT)
            {

                return;
            }

            frame->Registers[ip[1]] =
                frame->RTNS[idx];

        }
        static void CALL(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int target = reg[ip[2]];

            reg[ip[1]] = 0;

            if ((uint)target >= MAX_ArrayBlock || Metadatas[target].Exists == 0)
                return;

            if (FrameTop + 1 >= MAX_FRAME)
                return;




            FrameTop++;

            fixed (FRAME* pFrames = Frames)
            {
                FRAME* next = pFrames + FrameTop;
                *next = default;
                next->PrevArrayBlock = CurrentArrayBlock;
                next->PrevPC = PC;
                next->Self = target;

                for (int i = 0; i < Args_COUNT; i++)
                    next->ARGS[i] = frame->ARGS[i];
            }

            reg[ip[1]] = 1; // 호출부(이전 프레임) 레지스터에 성공 기록 — 컨텍스트 전환 전에 이미 계산됨

            CurrentArrayBlock = target;
            PC = 0;
        }
        static void EXIT(int* ip, FRAME* frame)
        {
            if (FrameTop <= 0)
                return; // 크래시 대신 조용히 무시

            fixed (FRAME* pFrames = Frames)
            {
                FRAME* current = pFrames + FrameTop;
                FRAME* prev = pFrames + (FrameTop - 1);

                for (int i = 0; i < rtns_COUNT; i++)
                    prev->RTNS[i] = current->RTNS[i];

                CurrentArrayBlock = current->PrevArrayBlock;
                PC = current->PrevPC;


            }

            FrameTop--;
        }
        static void DONE(int* ip, FRAME* frame)
        {
            EndRun = true;
        }
        static void RESZ(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int id =
                reg[ip[2]];

            int newLength =
                reg[ip[3]];

            reg[ip[1]] = 0;

            if ((uint)id >= MAX_ArrayBlock)
            {
                return;
            }



            if (newLength <= 0)
            {
                return;
            }

            if (Metadatas[id].Exists == 0)
            {
                return;
            }

            var oldHeader =
                Metadatas[id];

            int oldBase =
                oldHeader.Base;

            int oldLength =
                oldHeader.Length;

            // =====================================
            // 새 메모리 확보
            // =====================================

            if (newLength > MEMORY_SIZE - HeapTop)
            {
                Compact();

                if (newLength > MEMORY_SIZE - HeapTop)
                {
                    return;
                }
            }

            int newBase =
                HeapTop;

            HeapTop += newLength;

            // =====================================
            // 데이터 복사
            // =====================================

            int copyLength =
                oldLength < newLength
                ? oldLength
                : newLength;

            Array.Copy(
                MEMORY,
                oldBase,
                MEMORY,
                newBase,
                copyLength);

            // =====================================
            // 추가 영역 초기화
            // =====================================

            for (int i = copyLength;
                 i < newLength;
                 i++)
            {
                MEMORY[newBase + i] = 0;
            }

            // =====================================
            // 헤더 갱신
            // =====================================

            oldHeader.Base =
                newBase;

            oldHeader.Length =
                newLength;

            Metadatas[id] =
                oldHeader;

            frame->Registers[ip[1]] = 1;

        }
        static void ALOC(int* ip, FRAME* frame)
        {

            int* reg = frame->Registers;
            int len =
                reg[ip[2]];


            frame->Registers[ip[1]] =
        AllocateArrayBlock(len);




        }
        static void FREE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            bool ok = FreeArrayBlock(
                reg[ip[2]]); int id =
    reg[ip[2]];


            frame->Registers[ip[1]] =
                ok ? 1 : 0;
        }
        static void LOAD(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int id = reg[ip[2]];

            bool ok = LoadArrayBlock(id);

            frame->Registers[ip[1]] =
                ok ? 1 : 0;
        }
        static void SAVE(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;

            int id = reg[ip[2]];
            bool ok = SaveArrayBlock(id
                );

            frame->Registers[ip[1]] =
                ok ? 1 : 0;

        }
        static void COPY(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;


            int dstId =
                reg[ip[2]];

            int dstStart =
                reg[ip[3]];


            int srcId =
                reg[ip[5]];

            int srcStart =
                reg[ip[6]];


            int length =
                reg[ip[7]];



            reg[ip[1]] = 0;



            if (srcId < 0 || dstId < 0)
                return;


            if ((uint)srcId >= MAX_ArrayBlock ||
                (uint)dstId >= MAX_ArrayBlock)
                return;



            var src = Metadatas[srcId];
            var dst = Metadatas[dstId];



            if (src.Exists == 0 ||
                dst.Exists == 0)
                return;


            if (length <= 0)
                return;



            if (srcStart < 0 ||
                srcStart + length > src.Length)
                return;


            if (dstStart < 0 ||
                dstStart + length > dst.Length)
                return;



            int srcBase =
                src.Base + srcStart;

            int dstBase =
                dst.Base + dstStart;



            // MEMORY 내부 겹침 검사

            bool overlap =
                srcBase < dstBase + length &&
                dstBase < srcBase + length;



            if (overlap)
            {
                // 뒤에서부터 복사

                for (int i = length - 1; i >= 0; i--)
                {
                    MEMORY[dstBase + i] =
                        MEMORY[srcBase + i];
                }
            }
            else
            {
                // 일반 순차 복사

                for (int i = 0; i < length; i++)
                {
                    MEMORY[dstBase + i] =
                        MEMORY[srcBase + i];
                }
            }



            reg[ip[1]] = 1;


        }
        static void SWAP(int* ip, FRAME* frame)
        {


            int* reg =
                frame->Registers;

            int srcId =
                reg[ip[5]];

            int srcStart =
                reg[ip[6]];

            int dstId =
                reg[ip[2]];

            int dstStart =
                reg[ip[3]];

            reg[ip[1]] = 0;



            if ((uint)srcId >= MAX_ArrayBlock)
                return;

            if ((uint)dstId >= MAX_ArrayBlock)
                return;
            if (Metadatas[srcId].Exists == 0)
                return;

            if (Metadatas[dstId].Exists == 0)
                return;


            if (srcId < 0 ||
                dstId < 0)
                return;

            var src =
                Metadatas[srcId];

            var dst =
                Metadatas[dstId];

            int temp = MEMORY[src.Base + srcStart];
            MEMORY[src.Base + srcStart] = MEMORY[dst.Base + dstStart];
            MEMORY[dst.Base + dstStart] = temp;

            frame->Registers[ip[1]] = 1;
        }
        static void LNTH(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;
            int id = reg[ip[2]];


            if ((uint)id >= MAX_ArrayBlock)
                return;



            if (Metadatas[id].Exists == 0)
                return;


            if (id < 0)
                return;

            reg[ip[1]] = Metadatas[id].Length;

        }
        static void EXST(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;
            int id = reg[ip[2]];


            if ((uint)id >= MAX_ArrayBlock)
                return;

            if (id < 0)
                return;

            reg[ip[1]] = Metadatas[id].Exists;

        }

        static void BASE(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;
            int id = reg[ip[2]];


            if ((uint)id >= MAX_ArrayBlock)
                return;



            if (Metadatas[id].Exists == 0)
                return;


            if (id < 0)
                return;

            reg[ip[1]] = Metadatas[id].Base;

        }
        static void FILL(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];

            int value =
                reg[ip[6]];


            reg[ip[1]] = 0;

            if (ArrayBlockId < 0)
                return;


            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;



            if (Metadatas[ArrayBlockId].Exists == 0)
                return;



            var meta =
                Metadatas[ArrayBlockId];

            if (start < 0 ||
                length + start > meta.Length ||
                0 >= length)
                return;


            int baseAddr =
                meta.Base;

            for (int i = start; i < start + length; i++)
            {
                MEMORY[baseAddr + i] =
                    value;
            }

            reg[ip[1]] = 1;
        }
        static void SUMM(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];

            reg[ip[1]] = 0;

            long sum = 0;
            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;
            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;
            for (int i = start; i < start + length; i++)
            {
                sum +=
                    MEMORY[meta.Base + i];
            }

            reg[ip[1]] =
                (int)sum;
        }
        static void AVRG(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];


            reg[ip[1]] = 0;

            if (length <= 0)
            {
                reg[ip[1]] = 0;
                return;
            }

            long sum = 0;
            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;
            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;
            for (int i = start; i < start + length; i++)
            {
                sum +=
                    MEMORY[meta.Base + i];
            }

            reg[ip[1]] =
                (int)(sum / length);
        }
        static void MINM(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];

            reg[ip[1]] = 0;

            if (start >= start + length)
                return;

            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;
            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;
            int min =
                MEMORY[meta.Base + start];

            for (int i = start + 1; i < start + length; i++)
            {
                int v =
                    MEMORY[meta.Base + i];

                if (v < min)
                    min = v;
            }

            reg[ip[1]] =
                min;
        }
        static void MAXM(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];


            reg[ip[1]] = 0;

            if (start >= start + length)
                return;

            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;
            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;
            int max =
                MEMORY[meta.Base + start];

            for (int i = start + 1; i < start + length; i++)
            {
                int v =
                    MEMORY[meta.Base + i];

                if (v > max)
                    max = v;
            }

            reg[ip[1]] =
                max;
        }
        static void FIND(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];

            int value =
                reg[ip[6]];


            reg[ip[1]] =
                0;


            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;

            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;

            for (int i = start; i < start + length; i++)
            {
                if (MEMORY[meta.Base + i] == value)
                {
                    reg[ip[1]] = i;
                    return;
                }
            }
        }
        static void CONT(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            int length =
                reg[ip[5]];

            int value =
                reg[ip[6]];

            reg[ip[1]] = 0;

            int count = 0;

            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;
            var meta =
                Metadatas[ArrayBlockId];
            if (start < 0)
                return;

            if (start + length > meta.Length)
                return;

            if (start >= start + length)
                return;


            for (int i = start; i < start + length; i++)
            {
                if (MEMORY[meta.Base + i] == value)
                    count++;
            }

            reg[ip[1]] =
                count;
        }

        static void RNDM(int* ip, FRAME* frame)
        {
            int* reg = frame->Registers;
            int a = reg[ip[2]];
            int b = reg[ip[3]];
            if (a == b)
            {
                reg[ip[1]] = a;
            }
            else if (a > b)
            {
                reg[ip[1]] = RandomNumberGenerator.GetInt32(b, a);
            }
            else
            {
                reg[ip[1]] = RandomNumberGenerator.GetInt32(a, b);
            }
        }
        static void SORT(int* ip, FRAME* frame)
        {
            int* reg =
                frame->Registers;

            // ip[1] = 결과(성공/실패) 레지스터
            int ArrayBlockId =
                reg[ip[2]];

            int start =
                reg[ip[3]];

            // ip[4]는 EXTRA 슬롯의 opcode(NOP), ip[5]/ip[6]는 그 슬롯의 A/B필드
            int length =

                reg[ip[5]];

            int mode =
                reg[ip[6]]; // 0=오름차순 1=내림차순 2=무작위


            if ((uint)ArrayBlockId >= MAX_ArrayBlock)
                return;

            if (Metadatas[ArrayBlockId].Exists == 0)
                return;



            var meta =
                Metadatas[ArrayBlockId];

            if (start < 0 ||
                length <= 0 ||
                start + length > meta.Length)
                return;

            int baseAddr =
                meta.Base;

            if (mode == 2)
            {
                // Fisher-Yates 셔플
                for (int i = start + length - 1; i > start; i--)
                {
                    int j = start + _rng.Next(i - start + 1);

                    int tmp =
                        MEMORY[baseAddr + i];

                    MEMORY[baseAddr + i] =
                        MEMORY[baseAddr + j];

                    MEMORY[baseAddr + j] =
                        tmp;
                }

                frame->Registers[ip[1]] = 1;
                return;
            }

            bool descending = mode == 1;

            for (int i = start; i < start + length - 1; i++)
            {
                int pick =
                    i;

                for (int j = i + 1; j < start + length; j++)
                {
                    bool better = descending
                        ? MEMORY[baseAddr + j] > MEMORY[baseAddr + pick]
                        : MEMORY[baseAddr + j] < MEMORY[baseAddr + pick];

                    if (better)
                    {
                        pick = j;
                    }
                }

                if (pick == i)
                    continue;

                int temp =
                    MEMORY[baseAddr + i];

                MEMORY[baseAddr + i] =
                    MEMORY[baseAddr + pick];

                MEMORY[baseAddr + pick] =
                    temp;
            }

            frame->Registers[ip[1]] = 1;
        }

    }
}

 