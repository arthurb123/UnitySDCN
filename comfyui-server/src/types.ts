export type Segment = {
    maskImageBase64: string,
    description: string
};

export type ComfyUIConfiguration = {
    address: string,
    checkpointModelName: string,
    useControlNet: boolean,
    controlNetDepthModelName: string,
    controlNetDepthMode: ControlNetModelType,
    controlNetNormalModelName: string,
    controlNetNormalMode: ControlNetModelType,
    steps: number,
    cfg: number,
    sampler: string,
    scheduler: string,
    denoise: number
};

export type ControlNetModelType = 'normal' | 'lllite';