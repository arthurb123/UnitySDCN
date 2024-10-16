#nullable enable

namespace UnitySDCN {
    internal class SDCNSegment {
        // The mask of the segment
        internal byte[] MaskImage { get; private set; }
        // The base64 encoded mask of the segment
        internal string MaskImageBase64 => System.Convert.ToBase64String(MaskImage);
        // The SDCN object associated with the segment
        internal SDCNObject SDCNObject { get; private set; }

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
        internal string? GetDescription()
        {
            return SDCNObject != null 
                ? $"{SDCNObject.Description}"
                : null;
        }
    }
}