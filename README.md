# UnitySDCN Pipeline
Work-in-progress pipeline to generate high-quality visuals for barebones 3D environments 
in Unity through the usage of Stable Diffusion & ControlNet.

## Previes
Unity 3D demo scene        |  Post visualization
:-------------------------:|:-------------------------:
![](resources/pre-gen.png)  |  ![](resources/post-gen.png)

## Usage
1. Install the NPM packages in the `comfyui-server` folder using `npm i` (tested with NodeJS v20)
2. Run your ComfyUI instance, where you have the following custom node installed: https://github.com/Acly/comfyui-tooling-nodes
3. Edit the `comfyui-server/config.json` file such that all fields are correct (primarily, the address and model names)
4. Install the UnitySDCN package from the `unity-plugin` or open the demo scene located in the `unity-plugin` project.
5. Modify the scene and use the `SDCNManager` game object to generate images.

## Roadmap
TODO: Fill this section