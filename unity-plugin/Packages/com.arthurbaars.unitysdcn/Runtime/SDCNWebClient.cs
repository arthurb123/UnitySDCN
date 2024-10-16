#nullable enable

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

namespace UnitySDCN
{
    // Web client for the SDCN server
    internal static class SDCNWebClient {
        /**
        * Generate an image from a segmented image
        * and a list of segments
        * @param depthImage The depth image of the scene
        * @param segments The segments of the image
        * @param backgroundDescription The description of the background
        * @param negativeDescription The description of the negative
        * @return A task which will return the generated image
        */
        internal static async Task<Texture2D?> GenerateImage(
            byte[] depthImage,
            SDCNSegment[] segments,
            string backgroundDescription,
            string negativeDescription
        ) {
            // Convert the segments to a JSON string
            // in the form of a list where;
            // [
            //    { maskImageBase64: string, description: string },
            //    ...
            // ]
            StringBuilder segmentsDictJson = new();
            segmentsDictJson.Append("[");
            foreach (SDCNSegment segment in segments)
            {
                string key = $"\"{segment.MaskImageBase64}\"";
                string value = $"\"{segment.GetDescription()}\"";
                segmentsDictJson.Append($"{{ \"maskImageBase64\": {key}, \"description\": {value} }},");
            }

            // Remove the last comma
            if (segmentsDictJson.Length > 1)
                segmentsDictJson.Length--;
            segmentsDictJson.Append("]");

            // Get the server address from the SDCNManager instance
            if (SDCNManager.Instance == null)
            {
                SDCNLogger.Error(
                    typeof(SDCNWebClient),
                    "Could not generate image from segmented image, no SDCNManager instance found"
                );
                return null;
            }
            string serverAddress = SDCNManager.Instance.WebServerAddress;

            // Send a POST request to the web server /generate endpoint
            string generateEndpoint = $"{serverAddress}/generate";
            SDCNLogger.Log(
                typeof(SDCNWebClient),
                $"Using endpoint {generateEndpoint}",
                SDCNVerbosity.Verbose
            );

            // Construct the JSON body
            string jsonBody = $@"
            {{
                ""depthImage"": ""{System.Convert.ToBase64String(depthImage)}"",
                ""segments"": {segmentsDictJson},
                ""backgroundPrompt"": ""{backgroundDescription}"",
                ""negativePrompt"": ""{negativeDescription}""
            }}";

            // Log
            SDCNLogger.Log(
                typeof(SDCNWebClient),
                "Issuing image generation request, this may take a while.."
            );

            // Send the POST request with the manually constructed JSON body
            string? generationId = await PostRequest(generateEndpoint, jsonBody);
            if (generationId == null) {
                SDCNLogger.Error(
                    typeof(SDCNWebClient), 
                    "Failed to generate image from segmented image"
                );
                return null;
            }
            SDCNLogger.Log(
                typeof(SDCNWebClient),
                $"Successfully generated image with generation ID {generationId}",
                SDCNVerbosity.Minimal
            );

            // Send a GET request to the endpoint
            string getEndpoint = $"{serverAddress}/image/{generationId}";
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(getEndpoint);
            UnityWebRequestAsyncOperation operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            // Check if successful
            if (www.result != UnityWebRequest.Result.Success) {
                SDCNLogger.Error(
                    typeof(SDCNWebClient), 
                    $"Failed to get texture from endpoint {getEndpoint}, got response code {www.responseCode} with body {www.downloadHandler.text}"
                );
                return null;
            }
            else {
                // Return the texture
                return ((DownloadHandlerTexture)www.downloadHandler).texture;
            }
        }

        /**
        * Send a generic POST request to the server
        * @param url The URL to send the request to
        * @param body The body of the request
        * @return A task which will return the response body
        */
        private static async Task<string?> PostRequest(string url, string body)
        {
            // Create a new HTTP request
            UnityWebRequest request = new(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);

            // Attach body and headers
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            // Check for errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                SDCNLogger.Error(
                    typeof(SDCNWebClient),
                    $"Failed to send POST request to {url}, got response code {request.responseCode} with body {request.downloadHandler.text}"
                );
                return null;
            }

            // Return the response body
            return request.downloadHandler.text;
        }
    }
}