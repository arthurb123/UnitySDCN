export type Segment = {
    maskImageBase64: string,
    description: string
};

export type ComfyUIConfiguration = {
    address: string,
    checkpointModelName: string,
    useControlNet: boolean,
    controlNetDepthModelName: string,
    controlNetNormalModelName: string,
    steps: number,
    cfg: number,
    sampler: string,
    scheduler: string,
    denoise: number
};