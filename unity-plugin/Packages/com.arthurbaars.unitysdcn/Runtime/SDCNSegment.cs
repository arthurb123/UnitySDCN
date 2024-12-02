#nullable enable

namespace UnitySDCN {
    public class SDCNSegment {
        // The mask of the segment
        public byte[] MaskImage { get; private set; }
        // The base64 encoded mask of the segment
        public string MaskImageBase64 => System.Convert.ToBase64String(MaskImage);
        // The strength of the segment, defaults to 1.0
        public float Strength => SDCNObject != null ? SDCNObject.Strength : 1.0f;
        // The SDCN object associated with the segment
        public SDCNObject SDCNObject { get; private set; }

        /**
         * Create a new segment
         * @param maskImage The mask of the segment
         * @param sdcnObject The SDCN object associated with the segment
         */
        internal SDCNSegment(byte[] maskImage, SDCNObject sdcnObject) {
            MaskImage = maskImage;
            SDCNObject = sdcnObject;
        }

        /**
          * Get the description of the segment
          * @return The description of the segment
          */
        public string? GetDescription()
        {
            return SDCNObject != null 
                ? $"{SDCNObject.GetSanitizedDescription()}"
                : null;
        }
    }
}