import express from 'express';
import path from 'path';
import bodyParser from 'body-parser';
import fs from 'fs';
import { ComfyUIClient } from 'comfy-ui-client';
import commandLineArgs from 'command-line-args';
import commandLineUsage from 'command-line-usage';
import { Segment, ComfyUIConfiguration } from './types';
import Utils from './utils';
import ComfyUI from './comfyui';

// Define command line arguments
const optionDefinitions = [
    { name: 'help', alias: 'h', type: Boolean, defaultValue: false },
    { name: 'port', alias: 'p', type: Number, defaultValue: 9295 },
    { name: 'configuration', alias: 'c', type: String, defaultValue: 'config.json' }
] as commandLineArgs.OptionDefinition[];
const options = commandLineArgs(optionDefinitions);

// Print help if necessary
if (options.help) {
    const sections = [
        {
            header: 'ComfyUI backend for the UnitySDCN pipeline.',
            content: 'Allows for image generation using Stable Diffusion with ControlNet via ComfyUI for any UnitySDCN frontend.'
        },
        {
            header: 'Options',
            optionList: [
                {
                    name: 'help',
                    description: 'Print this usage guide.'
                },
                {
                    name: 'port',
                    typeLabel: '{underline port}',
                    description: 'The port to run the server on (default is 9295).'
                },
                {
                    name: 'configuration',
                    typeLabel: '{underline file}',
                    description: 'The configuration file for the ComfyUI generation settings (default is config.json).'
                }
            ]
        }
    ];
    const usage = commandLineUsage(sections);
    console.log(usage);
    process.exit();
}

// Define constants
const port = options.port as number;
const comfyUIConfiguration = JSON.parse(fs.readFileSync(options.configuration, 'utf8')) as ComfyUIConfiguration;

// Sanity check
if (comfyUIConfiguration.address.indexOf('http') !== -1) {
    console.error('Please provide the ComfyUI address without the protocol (http/https) as it uses WebSockets.');
    process.exit(1);
}

// Pretty print the configuration
console.log(`Using ComfyUI configuration '${options.configuration}':`);
console.log(`> Model:      ${comfyUIConfiguration.checkpointModelName}`);
if (comfyUIConfiguration.useControlNet)
console.log(`> CN Model:   ${comfyUIConfiguration.controlNetDepthModelName}`);
console.log(`> ControlNet: ${comfyUIConfiguration.useControlNet}`);
console.log(`> Steps:      ${comfyUIConfiguration.steps}`);
console.log(`> CFG:        ${comfyUIConfiguration.cfg}`);
console.log(`> Sampler:    ${comfyUIConfiguration.sampler}`);
console.log(`> Scheduler:  ${comfyUIConfiguration.scheduler}`);

// Setup server and ComfyUI client
const app = express();
// TODO: Do we need to make this configurable?
const comfyUIClientId = 'b3b7d4b6-3b9a-4c6e-9d2b-0b4d6b6f5b9c';
const comfyUIClient = new ComfyUIClient(comfyUIConfiguration.address, comfyUIClientId);

// Reset temp folder
if (fs.existsSync('temp'))
    fs.rmSync('temp', { recursive: true });
fs.mkdirSync('temp');

app.use(bodyParser.json({
    limit: '100mb'
}));

let currentGenerationId = 0;
app.post('/generate', async (req, res) => {
    try {
        // Extract data
        const width = req.body.width as number;
        const height = req.body.height as number;
        const segments = req.body.segments as Segment[];
        const depthImageBase64 = req.body.depthImage as string | undefined;
        const normalImageBase64 = req.body.normalImage as string | undefined;
        const backgroundPrompt = req.body.backgroundPrompt as string;
        const negativePrompt = req.body.negativePrompt as string;

        // Attempt to connect to ComfyUI with a timeout,
        // this is necessary as the library we use does not
        // resolve the promise if the connection fails
        const timeoutDuration = 5000; // 5 seconds
        const connectWithTimeout = new Promise<void>((resolve, reject) => {
            const timeout = setTimeout(() => {
                reject(new Error('Connection to ComfyUI timed out'));
            }, timeoutDuration);

            comfyUIClient.connect()
                .then(() => {
                    clearTimeout(timeout);
                    resolve();
                })
                .catch(reject);
        });
        await connectWithTimeout;
        console.log(`Connected to ComfyUI at ${comfyUIConfiguration.address}!`);

        // Create workflow
        console.log(`Creating dynamic workflow..`);
        let generationId = currentGenerationId++;
        const workflow = await ComfyUI.createWorkflow(
            generationId,
            width, height,
            segments,
            depthImageBase64,
            normalImageBase64,
            negativePrompt, 
            backgroundPrompt,
            comfyUIConfiguration
        );

        // Generate images
        console.log(`Starting image generation..`);
        const images = await comfyUIClient.getImages(workflow);

        // Save images to file
        const outputDir = './temp/output';
        if (!fs.existsSync(outputDir))
            fs.mkdirSync(outputDir);
        await comfyUIClient.saveImages(images, outputDir);

        // Find the image where the image name
        // is present in the file name
        const imageName = Utils.formatImageName(generationId);
        const imageLocation = Utils.formatImageLocation(generationId);
        let foundImage = false;
        fs.readdirSync(outputDir).forEach(file => {
            if (file.includes(imageName)) {
                // Set file to image name
                fs.renameSync(path.join(outputDir, file), imageLocation);
                foundImage = true;
            }
        });

        // Disconnect
        await comfyUIClient.disconnect();

        // Check if the image was found
        if (foundImage)
            console.log(`Image saved to ${imageLocation}!`);
        else {
            console.error(`Failed to find image with name ${imageName}, check ComfyUI logs for more information.`);
            res.sendStatus(500);
            return;
        }

        // Send back the generation id
        res.status(200).send(generationId.toString());
    } catch (error) {
        console.error(`Error generating image: ${error}`);
        res.sendStatus(500);
    }
});

app.get('/image/:id', (req, res) => {
    // Get the generation ID
    const id = parseInt(req.params.id);

    // Get the image location
    const imageLocation = Utils.formatImageLocation(id);

    // Check if the image exists
    if (!fs.existsSync(imageLocation)) {
        res.sendStatus(404);
        return;
    }

    // Send
    res.sendFile(imageLocation);
});

app.listen(port, () => {
    console.log(`UnitySDCN-ComfyUI web server listening on port *:${port}`);
});