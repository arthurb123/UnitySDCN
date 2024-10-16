import { Prompt } from "comfy-ui-client";
import { PNG } from "pngjs";
import fs from 'fs';
import { Segment, ComfyUIConfiguration } from "./types";
import Utils from "./utils";

export default class ComfyUI {
    static async createWorkflow (
        generationId: number,
        depthImageBase64: string,
        segments: Segment[],
        negativePrompt: string,
        backgroundPrompt: string,
        comfyUIConfiguration: ComfyUIConfiguration
    ) : Promise<Prompt> {
        // Decode the base64 image into a buffer
        const depthImageBuffer = Buffer.from(depthImageBase64, 'base64');
    
        // Parse the PNG image using pngjs
        const png = PNG.sync.read(depthImageBuffer);
    
        // Get image dimensions and pixel data
        const { width, height, data } = png;
    
        // Set generation settings
        const generationSettings = {
            imageWidth: width,
            imageHeight: height,
            seed: Math.floor(Math.random() * 1000000000),
            // Don't forget the user-defined ComfyUI configuration
            ...comfyUIConfiguration
        };
    
        // Setup base workflow
        const MODEL_ID = 1;
        const CONTROLNET_MODEL_ID = 2;
        const DEPTH_IMAGE_ID = 3;
        const NEGATIVE_CONDITIONING_ID = 4;
        const REGION_ATTENTION_MASK_ID = 5;
        const KSAMPLER_ID = 6;
        const LATENT_IMAGE_ID = 7;
        const VAE_DECODE_ID = 8;
        const SAVE_IMAGE_ID = 9;
        let nodeCounter = 10;
    
        // Create background region partial workflow
        const {
            backgroundRegionPartialWorkflow,
            backgroundRegionConditioningId,
            backgroundRegionId
        } = generationSettings.useControlNet
            ? ComfyUI.createWorkflowBackgroundRegionWithControlNet(
                nodeCounter, 
                backgroundPrompt, 
                MODEL_ID,
                CONTROLNET_MODEL_ID,
                DEPTH_IMAGE_ID
            )
            : ComfyUI.createWorkflowBackgroundRegion(
                nodeCounter, 
                backgroundPrompt, 
                MODEL_ID
            );
        nodeCounter = backgroundRegionId + 1;
    
        // Create multiple partial workflows for each
        // color segment
        let regionPartialWorkflows = '';
        let currentRegionId = backgroundRegionId;
        for (const segment of segments) {
            // Create a partial workflow for the segment
            const {
                partialWorkflow,
                regionId
            } = generationSettings.useControlNet
                ? ComfyUI.createWorkflowRegionInputWithControlNet(
                    nodeCounter,
                    segment.maskImageBase64,
                    segment.description,
                    MODEL_ID,
                    CONTROLNET_MODEL_ID,
                    DEPTH_IMAGE_ID,
                    currentRegionId
                )
                : ComfyUI.createWorkflowRegionInput(
                    nodeCounter,
                    segment.maskImageBase64,
                    segment.description,
                    MODEL_ID,
                    currentRegionId
                );
    
            // Update current region id
            currentRegionId = regionId;
    
            // Set node id counter
            nodeCounter = regionId + 1;
    
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
            ${comfyUIConfiguration.useControlNet ? `
            "${CONTROLNET_MODEL_ID}": {
                "inputs": {
                    "control_net_name": "${generationSettings.controlNetDepthModelName}"
                },
                "class_type": "ControlNetLoader",
                    "_meta": {
                    "title": "Load ControlNet Model"
                }
            },
            "${DEPTH_IMAGE_ID}": {
                "inputs": {
                    "image": "${depthImageBase64}"
                },
                "class_type": "ETN_LoadImageBase64",
                "_meta": {
                    "title": "Load Image (Base64)"
                }
            },` : ''}
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
                    "filename_prefix": "${Utils.formatImageName(generationId)}",
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
                        "${MODEL_ID}",
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
        modelId: number,
        regionId?: number
    ): { partialWorkflow: string, regionId: number } {
        const loadMaskId = nodeId++;
        const conditioningId = nodeId++;
        const newRegionId = nodeId++;
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
                "${newRegionId}": {
                    "inputs": {
                        "mask": [
                            "${loadMaskId}",
                            0
                        ],
                        "conditioning": [
                            "${conditioningId}",
                            0
                        ],
                        "regions": [
                            ${regionId == null
                                ? ''
                                : `"${regionId}", 0`
                            }
                        ]
                    },
                    "class_type": "ETN_DefineRegion",
                    "_meta": {
                        "title": "Define Region"
                    }
                },
            `.trim(),
            regionId: newRegionId
        };
    };
    
    private static createWorkflowRegionInputWithControlNet(
        nodeId: number,
        maskBase64: string, 
        prompt: string, 
        modelId: number,
        controlNetModelId: number,
        depthImageId: number,
        regionId?: number
    ): { partialWorkflow: string, regionId: number } {
        const loadMaskId = nodeId++;
        const conditioningId = nodeId++;
        const applyControlNetId = nodeId++;
        const newRegionId = nodeId++;
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
                "${applyControlNetId}": {
                    "inputs": {
                        "strength": 1,
                        "conditioning": [
                            "${conditioningId}",
                            0
                        ],
                        "control_net": [
                            "${controlNetModelId}",
                            0
                        ],
                        "image": [
                            "${depthImageId}",
                            0
                        ]
                    },
                    "class_type": "ControlNetApply",
                        "_meta": {
                        "title": "Apply ControlNet"
                    }
                },
                "${newRegionId}": {
                    "inputs": {
                        "mask": [
                            "${loadMaskId}",
                            0
                        ],
                        "conditioning": [
                            "${applyControlNetId}",
                            0
                        ],
                        "regions": [
                            ${regionId == null
                                ? ''
                                : `"${regionId}", 0`
                            }
                        ]
                    },
                    "class_type": "ETN_DefineRegion",
                    "_meta": {
                        "title": "Define Region"
                    }
                },
            `.trim(),
            regionId: newRegionId
        };
    };

    private static createWorkflowBackgroundRegion(
        nodeId: number,
        prompt: string, 
        modelId: number
    ): { 
        backgroundRegionPartialWorkflow: string, 
        backgroundRegionConditioningId: number, 
        backgroundRegionId: number 
    } {
        const conditioningId = nodeId++;
        const regionId = nodeId++;
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
                "${regionId}": {
                    "inputs": {
                        "conditioning": [
                            "${conditioningId}",
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
            backgroundRegionId: regionId
        };
    };
    
    private static createWorkflowBackgroundRegionWithControlNet(
        nodeId: number,
        prompt: string, 
        modelId: number,
        controlNetModelId: number,
        depthImageId: number
    ): { 
        backgroundRegionPartialWorkflow: string, 
        backgroundRegionConditioningId: number, 
        backgroundRegionId: number 
    } {
        const conditioningId = nodeId++;
        const applyControlNetId = nodeId++;
        const regionId = nodeId++;
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
                "${applyControlNetId}": {
                    "inputs": {
                        "strength": 1,
                        "conditioning": [
                            "${conditioningId}",
                            0
                        ],
                        "control_net": [
                            "${controlNetModelId}",
                            0
                        ],
                        "image": [
                            "${depthImageId}",
                            0
                        ]
                    },
                    "class_type": "ControlNetApply",
                        "_meta": {
                        "title": "Apply ControlNet"
                    }
                },
                "${regionId}": {
                    "inputs": {
                        "conditioning": [
                            "${applyControlNetId}",
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
            backgroundRegionId: regionId
        };
    };
}