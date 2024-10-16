export type Segment = {
    maskImageBase64: string,
    description: string
};

export type ComfyUIConfiguration = {
    address: string,
    checkpointModelName: string,
    controlNetDepthModelName: string,
    useControlNet: boolean,
    steps: number,
    cfg: number,
    sampler: string,
    scheduler: string,
    denoise: number
};