import path from 'path';

export default class Utils {
    static formatImageName = (id: number) => `UnitySDCN_${id}.png`;
    static formatImageLocation = (id: number) => path.join(__dirname, '../temp/output', Utils.formatImageName(id));
}