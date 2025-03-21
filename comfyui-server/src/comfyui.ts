import { Prompt } from "comfy-ui-client";
import fs from 'fs';
import { Segment, ComfyUIConfiguration } from "./types";
import Utils from "./utils";

export default class ComfyUI {
    static async createWorkflow (
        comfyUIConfiguration: ComfyUIConfiguration,
        generationId: number,
        width: number, height: number,
        segments: Segment[],
        negativePrompt: string,
        backgroundPrompt: string,
        depthImageBase64: string | undefined,
        normalImageBase64: string | undefined,
        seed: number | undefined
    ) : Promise<Prompt> {
        // Attempt to decode the optional base64
        // images to buffers
        const depthImageBuffer = 
            depthImageBase64 != null 
                ? Buffer.from(depthImageBase64, 'base64') 
                : null;
        const normalImageBuffer =
            normalImageBase64 != null
                ? Buffer.from(normalImageBase64, 'base64')
                : null;
    
        // Set generation settings
        const generationSettings = {
            imageWidth: width,
            imageHeight: height,
            seed: seed ?? Math.floor(Math.random() * 1000000000),
            // Don't forget the user-defined ComfyUI configuration!
            ...comfyUIConfiguration
        };
    
        // Setup base workflow
        let nodeCounter = 0;
        const MODEL_ID                 = nodeCounter++;
        const NEGATIVE_CONDITIONING_ID = nodeCounter++;
        const REGION_ATTENTION_MASK_ID = nodeCounter++;
        const KSAMPLER_ID              = nodeCounter++;
        const LATENT_IMAGE_ID          = nodeCounter++;
        const VAE_DECODE_ID            = nodeCounter++;
        const SAVE_IMAGE_ID            = nodeCounter++;

        // Handle ControlNet usability
        const useControlNet              = comfyUIConfiguration.useControlNet;
        const useControlNetNormals       = useControlNet && normalImageBuffer != null;
        const useControlNetDepth         = useControlNet && depthImageBuffer  != null;

        const CONTROLNET_DEPTH_IMAGE_ID  = useControlNetDepth   ? nodeCounter++ : -1;
        const CONTROLNET_NORMAL_IMAGE_ID = useControlNetNormals ? nodeCounter++ : -1;
        const CONTROLNET_DEPTH_MODEL_ID  = 
            (comfyUIConfiguration.controlNetDepthMode === 'normal' && useControlNetDepth) 
                ? nodeCounter++ 
                : -1;
        const CONTROLNET_NORMAL_MODEL_ID = 
            (comfyUIConfiguration.controlNetNormalMode === 'normal' && useControlNetNormals) 
                ? nodeCounter++ 
                : -1;

        // Create partial workflows for the loading of
        // either normal controlnet models, or piped LLLite 
        // models based on the specified mode
        let partialControlNetWorkflow = '';
        let guidingModelId = MODEL_ID;
        if (useControlNetDepth) {
            switch (comfyUIConfiguration.controlNetDepthMode) {
                case 'normal':
                    partialControlNetWorkflow += 
                        `"${CONTROLNET_DEPTH_MODEL_ID}": {
                            "inputs": {
                                "control_net_name": "${generationSettings.controlNetDepthModelName}"
                            },
                            "class_type": "ControlNetLoader",
                                "_meta": {
                                "title": "Load ControlNet Model"
                            }
                        },`.trim();
                break;
                case 'lllite':
                    const {
                        partialWorkflow,
                        loadLLLiteId
                    } = ComfyUI.pipeModelThroughLLLiteControlNet(
                        nodeCounter++,
                        guidingModelId,
                        CONTROLNET_DEPTH_IMAGE_ID,
                        comfyUIConfiguration.controlNetDepthModelName,
                        comfyUIConfiguration.controlNetDepthStrength
                    );
        
                    partialControlNetWorkflow += partialWorkflow;
                    guidingModelId = loadLLLiteId;
                break;
            }
        }
        if (useControlNetNormals) {
            switch (comfyUIConfiguration.controlNetNormalMode) {
                case 'normal':
                    partialControlNetWorkflow += 
                        `"${CONTROLNET_NORMAL_MODEL_ID}": {
                            "inputs": {
                                "control_net_name": "${generationSettings.controlNetNormalModelName}"
                            },
                            "class_type": "ControlNetLoader",
                                "_meta": {
                                "title": "Load ControlNet Model"
                            }
                        },`.trim();
                break;
                case 'lllite':
                    const {
                        partialWorkflow,
                        loadLLLiteId
                    } = ComfyUI.pipeModelThroughLLLiteControlNet(
                        nodeCounter++,
                        guidingModelId,
                        CONTROLNET_NORMAL_IMAGE_ID,
                        comfyUIConfiguration.controlNetNormalModelName,
                        comfyUIConfiguration.controlNetNormalStrength
                    );
        
                    partialControlNetWorkflow += partialWorkflow;
                    guidingModelId = loadLLLiteId;
                break;
            }
        }
    
        // Create background region partial workflow
        const {
            backgroundRegionPartialWorkflow,
            backgroundRegionConditioningId,
            backgroundRegionId,
            nodeId
        } = ComfyUI.createWorkflowBackgroundRegion(
            nodeCounter, 
            backgroundPrompt,
            MODEL_ID,
            NEGATIVE_CONDITIONING_ID,
            CONTROLNET_DEPTH_MODEL_ID,
            CONTROLNET_DEPTH_IMAGE_ID,
            comfyUIConfiguration.controlNetDepthStrength,
            CONTROLNET_NORMAL_MODEL_ID,
            CONTROLNET_NORMAL_IMAGE_ID,
            comfyUIConfiguration.controlNetNormalStrength
        );
        nodeCounter = nodeId + 1;
    
        // Create multiple partial workflows for each
        // color segment
        let regionPartialWorkflows = '';
        let currentRegionId = backgroundRegionId;
        for (const segment of segments) {
            // Create a partial workflow for the segment
            const {
                partialWorkflow,
                regionId,
                nodeId
            } = ComfyUI.createWorkflowRegionInput(
                nodeCounter,
                segment.maskImageBase64,
                segment.description,
                segment.strength,
                MODEL_ID,
                NEGATIVE_CONDITIONING_ID,
                CONTROLNET_DEPTH_MODEL_ID,
                CONTROLNET_DEPTH_IMAGE_ID,
                comfyUIConfiguration.controlNetDepthStrength,
                CONTROLNET_NORMAL_MODEL_ID,
                CONTROLNET_NORMAL_IMAGE_ID,
                comfyUIConfiguration.controlNetNormalStrength,
                currentRegionId
            );
    
            // Update current region id
            currentRegionId = regionId;
    
            // Update node counter
            nodeCounter = nodeId + 1;
    
            // Append to workflow
            regionPartialWorkflows += partialWorkflow;
        }
    
        // Remove last comma from region partial workflows
        regionPartialWorkflows = regionPartialWorkflows.slice(0, -1);
    
        // Return the workflow
        const WORKFLOW = `
        {
            "${MODEL_ID}": {
                "inputs": {
                    "ckpt_name": "${generationSettings.checkpointModelName}"
                },
                "class_type": "CheckpointLoaderSimple",
                "_meta": {
                    "title": "Load Checkpoint"
                }
            },
            ${partialControlNetWorkflow}
            ${useControlNetDepth
            ? `"${CONTROLNET_DEPTH_IMAGE_ID}": {
                    "inputs": {
                        "image": "${depthImageBase64}"
                    },
                    "class_type": "ETN_LoadImageBase64",
                    "_meta": {
                        "title": "Load Image (Base64)"
                    }
                },`
            : ''}
            ${useControlNetNormals
            ? `"${CONTROLNET_NORMAL_IMAGE_ID}": {
                    "inputs": {
                        "image": "${normalImageBase64}"
                    },
                    "class_type": "ETN_LoadImageBase64",
                    "_meta": {
                        "title": "Load Image (Base64)"
                    }
                },`
            : ''}
            "${NEGATIVE_CONDITIONING_ID}": {
                "inputs": {
                    "text": "${negativePrompt}",
                    "clip": [
                        "${MODEL_ID}",
                        1
                    ]
                },
                "class_type": "CLIPTextEncode",
                "_meta": {
                    "title": "CLIP Text Encode (Prompt)"
                }
            },
            "${KSAMPLER_ID}": {
                "inputs": {
                    "seed": ${generationSettings.seed},
                    "steps": ${generationSettings.steps},
                    "cfg": ${generationSettings.cfg},
                    "sampler_name": "${generationSettings.sampler}",
                    "scheduler": "${generationSettings.scheduler}",
                    "denoise": ${generationSettings.denoise},
                    "model": [
                        "${REGION_ATTENTION_MASK_ID}",
                        0
                    ],
                    "positive": [
                        "${backgroundRegionConditioningId}",
                        0
                    ],
                    "negative": [
                        "${NEGATIVE_CONDITIONING_ID}",
                        0
                    ],
                    "latent_image": [
                        "${LATENT_IMAGE_ID}",
                        0
                    ]
                },
                "class_type": "KSampler",
                "_meta": {
                    "title": "KSampler"
                }
            },
            "${LATENT_IMAGE_ID}": {
                "inputs": {
                    "width": ${generationSettings.imageWidth},
                    "height": ${generationSettings.imageHeight},
                    "batch_size": 1
                },
                "class_type": "EmptyLatentImage",
                "_meta": {
                    "title": "Empty Latent Image"
                }
            },
            "${VAE_DECODE_ID}": {
                "inputs": {
                    "samples": [
                        "${KSAMPLER_ID}",
                        0
                    ],
                    "vae": [
                        "${MODEL_ID}",
                        2
                    ]
                },
                "class_type": "VAEDecode",
                "_meta": {
                    "title": "VAE Decode"
                }
            },
            "${SAVE_IMAGE_ID}": {
                "inputs": {
                    "filename_prefix": "${Utils.formatImageName(comfyUIConfiguration.outputPrefix, generationId)}",
                    "images": [
                        "${VAE_DECODE_ID}",
                        0
                    ]
                },
                "class_type": "SaveImage",
                "_meta": {
                    "title": "Save Image"
                }
            },
            "${REGION_ATTENTION_MASK_ID}": {
                "inputs": {
                    "model": [
                        "${guidingModelId}",
                        0
                    ],
                    "regions": [
                        "${currentRegionId}",
                        0
                    ]
                },
                "class_type": "ETN_AttentionMask",
                "_meta": {
                    "title": "Regions Attention Mask"
                }
            },
            ${backgroundRegionPartialWorkflow}
            ${regionPartialWorkflows}
        }`.trim();
    
        // Check if workflows folder exists
        if (!fs.existsSync('temp/workflows'))
            fs.mkdirSync('temp/workflows');
    
        // Save workflow
        fs.writeFileSync(`temp/workflows/${generationId}.json`, WORKFLOW);
    
        // Return the workflow
        return JSON.parse(WORKFLOW) as Prompt;
    };
    
    private static createWorkflowRegionInput(
        nodeId: number,
        maskBase64: string, 
        prompt: string,
        strength: number,
        modelId: number,
        negativeConditioningId: number,
        controlNetDepthModelId: number,
        controlNetDepthImageId: number,
        controlNetDepthStrength: number,
        controlNetNormalModelId: number,
        controlNetNormalImageId: number,
        controlNetNormalStrength: number,
        regionId?: number
    ): { partialWorkflow: string, regionId: number, nodeId: number } {
        const loadMaskId = nodeId++;
        const conditioningId = nodeId++;
        const newRegionId = nodeId++;
        const pipedControlNetDepth = ComfyUI.pipeConditioningThroughControlNet(
            nodeId++,
            modelId,
            conditioningId,
            negativeConditioningId,
            controlNetDepthImageId,
            controlNetDepthModelId,
            controlNetDepthStrength
        );
        const pipedControlNetNormal = ComfyUI.pipeConditioningThroughControlNet(
            pipedControlNetDepth.nodeId + 1,
            modelId,
            pipedControlNetDepth.conditioningId,
            negativeConditioningId,
            controlNetNormalImageId,
            controlNetNormalModelId,
            controlNetNormalStrength
        );
        return {
            partialWorkflow: `
                "${loadMaskId}": {
                    "inputs": {
                        "mask": "${maskBase64}"
                    },
                    "class_type": "ETN_LoadMaskBase64",
                    "_meta": {
                        "title": "Load Mask (Base64)"
                    }
                },
                "${conditioningId}": {
                    "inputs": {
                        "text": "${prompt}",
                        "clip": [
                            "${modelId}",
                            1
                        ]
                    },
                    "class_type": "CLIPTextEncode",
                    "_meta": {
                        "title": "CLIP Text Encode (Prompt)"
                    }
                },
                ${pipedControlNetDepth.partialWorkflow}
                ${pipedControlNetNormal.partialWorkflow}
                "${newRegionId}": {
                    "inputs": {
                        "mask": [
                            "${loadMaskId}",
                            0
                        ],
                        "conditioning": [
                            "${pipedControlNetNormal.conditioningId}",
                            0
                        ],
                        "regions": [
                            ${regionId == null
                                ? ''
                                : `"${regionId}", 0`
                            }
                        ],
                        "strength": ${strength}
                    },
                    "class_type": "ETN_DefineRegion",
                    "_meta": {
                        "title": "Define Region"
                    }
                },
            `.trim(),
            regionId: newRegionId,
            nodeId: pipedControlNetNormal.nodeId
        };
    };
    
    private static createWorkflowBackgroundRegion(
        nodeId: number,
        prompt: string, 
        modelId: number,
        negativeConditioningId: number,
        controlNetDepthModelId: number,
        controlNetDepthImageId: number,
        controlNetDepthStrength: number,
        controlNetNormalModelId: number,
        controlNetNormalImageId: number,
        controlNetNormalStrength: number
    ): { 
        backgroundRegionPartialWorkflow: string, 
        backgroundRegionConditioningId: number, 
        backgroundRegionId: number,
        nodeId: number
    } {
        const conditioningId = nodeId++;
        const regionId = nodeId++;
        const pipedControlNetDepth = ComfyUI.pipeConditioningThroughControlNet(
            nodeId++,
            modelId,
            conditioningId,
            negativeConditioningId,
            controlNetDepthImageId,
            controlNetDepthModelId,
            controlNetDepthStrength
        );
        const pipedControlNetNormal = ComfyUI.pipeConditioningThroughControlNet(
            pipedControlNetDepth.nodeId + 1,
            modelId,
            pipedControlNetDepth.conditioningId,
            negativeConditioningId,
            controlNetNormalImageId,
            controlNetNormalModelId,
            controlNetNormalStrength
        );
        return {
            backgroundRegionPartialWorkflow: `
                "${conditioningId}": {
                    "inputs": {
                        "text": "${prompt}",
                        "clip": [
                            "${modelId}",
                            1
                        ]
                    },
                    "class_type": "CLIPTextEncode",
                    "_meta": {
                        "title": "CLIP Text Encode (Prompt)"
                    }
                },
                ${pipedControlNetDepth.partialWorkflow}
                ${pipedControlNetNormal.partialWorkflow}
                "${regionId}": {
                    "inputs": {
                        "conditioning": [
                            "${pipedControlNetNormal.conditioningId}",
                            0
                        ]
                    },
                    "class_type": "ETN_BackgroundRegion",
                    "_meta": {
                        "title": "Background Region"
                    }
                },
            `.trim(),
            backgroundRegionConditioningId: conditioningId,
            backgroundRegionId: regionId,
            nodeId: pipedControlNetNormal.nodeId
        };
    };

    private static pipeModelThroughLLLiteControlNet(
        nodeId: number,
        modelId: number,
        controlNetImageId: number,
        controlNetModelName: string,
        strength: number = 1
    ): {
        partialWorkflow: string,
        loadLLLiteId: number
    } {
        // Create partial workflow
        const loadLLLiteId = nodeId++;
        return {
            partialWorkflow: `
                "${loadLLLiteId}": {
                    "inputs": {
                        "strength": ${strength},
                        "steps": 0,
                        "start_percent": 0,
                        "end_percent": 100,
                        "model": [
                            "${modelId}",
                            0
                        ],
                        "cond_image": [
                            "${controlNetImageId}",
                            0
                        ],
                        "model_name": "${controlNetModelName}"
                    },
                    "class_type": "LLLiteLoader",
                    "_meta": {
                        "title": "Load LLLite"
                    }
                },
            `.trim(),
            loadLLLiteId: loadLLLiteId
        }
    }

    private static pipeConditioningThroughControlNet(
        nodeId: number,
        modelId: number,
        conditioningId: number,
        negativeConditioningId: number,
        controlNetImageId: number,
        controlNetModelId: number,
        strength: number = 1
    ): { 
        partialWorkflow: string, 
        conditioningId: number, 
        nodeId: number 
    } {
        // Check if the control net is usable, if not
        // we just return an empty partial workflow
        // and redirect the pipe to the conditioning id
        if (controlNetImageId === -1 || controlNetModelId === -1)
            return { 
                partialWorkflow: '',
                conditioningId: conditioningId,
                nodeId: nodeId
            };

        // Create the partial workflow
        const applyControlNetId = nodeId++;
        return {
            partialWorkflow: `
                "${applyControlNetId}": {
                    "inputs": {
                        "strength": ${strength},
                        "start_percent": 0,
                        "end_percent": 1,
                        "positive": [
                            "${conditioningId}",
                            0
                        ],
                        "negative": [
                            "${negativeConditioningId}",
                            0
                        ],
                        "vae": [
                            "${modelId}",
                            2
                        ],
                        "control_net": [
                            "${controlNetModelId}",
                            0
                        ],
                        "image": [
                            "${controlNetImageId}",
                            0
                        ]
                    },
                    "class_type": "ControlNetApplyAdvanced",
                        "_meta": {
                        "title": "Apply ControlNet"
                    }
                },
            `.trim(),
            conditioningId: applyControlNetId,
            nodeId: applyControlNetId
        };
    };
}