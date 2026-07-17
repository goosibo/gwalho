

namespace Gwalho
{
    public static class GWVM_API
    {
        /// <summary>
        /// 데이터 Array 읽기
        /// </summary>
        public static bool Read(
            int arrayID,
            int index,
            out int value)
        {
            value = 0;


            if ((uint)arrayID >= GWVM.MAX_ArrayBlock)
                return false;

            ref METADATA meta = ref GWVM.Metadatas[arrayID];

            if (meta.Exists == 0)
                return false;


            if ((uint)index >= (uint)meta.Length)
                return false;

            value = GWVM.MEMORY[meta.Base + index];

            return true;
        }

        /// <summary>
        /// 데이터 Array 쓰기
        /// </summary>
        public static bool Write(
            int arrayID,
            int index,
            int value)
        {
            if (GWVM.State != VMState.Running)
                return false;

            if ((uint)arrayID >= GWVM.MAX_ArrayBlock)
                return false;

            ref METADATA meta = ref GWVM.Metadatas[arrayID];

            if (meta.Exists == 0)
                return false;


            if ((uint)index >= (uint)meta.Length)
                return false;

            GWVM.MEMORY[meta.Base + index] = value;

            return true;
        }

        /// <summary>
        /// 데이터 Array 길이
        /// </summary>
        public static bool GetLength(
            int arrayID,
            out int length)
        {
            length = 0;

            if ((uint)arrayID >= GWVM.MAX_ArrayBlock)
                return false;

            ref METADATA meta = ref GWVM.Metadatas[arrayID];

            if (meta.Exists == 0)
                return false;



            length = meta.Length;

            return true;
        }

        /// <summary>
        /// Array 존재 여부
        /// </summary>
        public static bool Exists(int arrayID)
        {
            if ((uint)arrayID >= GWVM.MAX_ArrayBlock)
                return false;

            return GWVM.Metadatas[arrayID].Exists != 0;
        }

        /// <summary>
        /// Logic Array 여부
        /// </summary>


        /// <summary>
        /// Static Array 여부
        /// </summary>


        /// <summary>
        /// Compact 진행 여부
        /// </summary>

        /// <summary>
        /// 메타데이터 얻기
        /// </summary>
        public static bool GetMetadata(
            int arrayID,
            out METADATA metadata)
        {
            metadata = default;

            if ((uint)arrayID >= GWVM.MAX_ArrayBlock)
                return false;

            metadata = GWVM.Metadatas[arrayID];

            return metadata.Exists != 0;
        }
    }
}