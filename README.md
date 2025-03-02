# UnitySDCN Pipeline
Level visualization pipeline to generate high-quality visuals for barebones 3D environments 
in Unity URP through the usage of [Stable Diffusion](https://arxiv.org/abs/2112.10752) & [ControlNet](https://arxiv.org/abs/2302.05543), for my [MSc. Game & Media Technology](https://www.uu.nl/en/masters/game-and-media-technology)
thesis at [Utrecht University](https://www.uu.nl/en).

## Preview
Unity URP demo scene       |  Post visualization
:-------------------------:|:-------------------------:
![](resources/pre-gen.png) |  ![](resources/post-gen.png)

## Usage
1. Install the NPM packages in the `comfyui-server` folder using `npm i` (tested with Node v20)
2. Run your ComfyUI instance, where you have the following custom nodes installed: 
    - [ComfyUI-Tooling-Nodes](https://github.com/arthurb123/comfyui-tooling-nodes)
    - [ControlNet-LLLite-ComfyUI](https://github.com/arthurb123/ControlNet-LLLite-ComfyUI)
3. Edit the `comfyui-server/config.json` file such that all fields are correct (primarily, the address and model names)
4. Install the UnitySDCN package from the `unity-plugin/Packages/UnitySDCN` folder to your URP project, or open the demo scene located in the `unity-plugin` project.
5. Modify the scene and use the `SDCNManager` game object to generate images.

## Models
<ins>ControlNet SDXL</ins>
* Depth: https://huggingface.co/xinsir/controlnet-depth-sdxl-1.0
* Normal: https://huggingface.co/Eugeoter/noob-sdxl-controlnet-normal

<ins>ControlNet SD1.5</ins>
* Depth: https://huggingface.co/lllyasviel/sd-controlnet-depth
* Normal: https://huggingface.co/lllyasviel/sd-controlnet-normal
