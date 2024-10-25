# UnitySDCN Pipeline
Work-in-progress pipeline to generate high-quality visuals for barebones 3D environments 
in Unity URP through the usage of Stable Diffusion & ControlNet.

## Preview
Unity URP demo scene       |  Post visualization
:-------------------------:|:-------------------------:
![](resources/pre-gen.png) |  ![](resources/post-gen.png)

## Usage
1. Install the NPM packages in the `comfyui-server` folder using `npm i` (tested with NodeJS v20)
2. Run your ComfyUI instance, where you have the following custom node installed: https://github.com/Acly/comfyui-tooling-nodes
3. Edit the `comfyui-server/config.json` file such that all fields are correct (primarily, the address and model names)
4. Install the UnitySDCN package from the `unity-plugin/Packages/UnitySDCN` folder to your URP project, or open the demo scene located in the `unity-plugin` project.
5. Modify the scene and use the `SDCNManager` game object to generate images.

## Models
<u>ControlNet Flux.1</u>
* Depth: https://huggingface.co/jasperai/Flux.1-dev-Controlnet-Depth
* Normal: https://huggingface.co/jasperai/Flux.1-dev-Controlnet-Surface-Normals

<u>ControlNet SD1.5</u>
* Depth: https://huggingface.co/lllyasviel/sd-controlnet-depth
* Normal: https://huggingface.co/lllyasviel/sd-controlnet-normal

<u>ControlNet SDXL</u>
* Depth: https://huggingface.co/diffusers/controlnet-depth-sdxl-1.0
* Normal: <i>NO (WORKING) MODEL AVAILABLE</i>

Unfortunately, Normals are not easily supported for SDXL. There only exists
a so called "LLLite" model but this requires a custom node in ComfyUI which
is a lot of extra work to get it to work in this pipeline.

Therefore, if you are using an SDXL model - please disable Normal mode in the
SDCNCamera settings in the Unity frontend.

## Roadmap
* Add region ordering based on camera-object distance metric
* Add SDXL normal support using LLLite with: https://github.com/kohya-ss/ControlNet-LLLite-ComfyUI