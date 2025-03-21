import path from 'path';

export default class Utils {
    static formatImageName = (prefix: string, id: number) => `${prefix}_${id}.png`;
    static formatImageLocation = (prefix: string, id: number) => path.join(__dirname, '../temp/output', Utils.formatImageName(prefix, id));
}