export type Segment = {
    maskImageBase64: string,
    description: string,
    strength: number
};

export type ComfyUIConfiguration = {
    address: string,
    checkpointModelName: string,

    useControlNet: boolean,
    controlNetDepthModelName: string,
    controlNetDepthMode: ControlNetModelType,
    controlNetDepthStrength: number,
    controlNetNormalModelName: string,
    controlNetNormalMode: ControlNetModelType,
    controlNetNormalStrength: number,
    
    steps: number,
    cfg: number,
    sampler: string,
    scheduler: string,
    denoise: number,

    outputPrefix: string
};

export type ControlNetModelType = 'normal' | 'lllite';