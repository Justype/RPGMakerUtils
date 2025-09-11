# Version History

## Version 2.2.1 RPG Maker Utils

- 支持切换不同版本的翻译插件
  - `Faster` 更快的翻译速度，部分菜单可能未被翻译
  - `Broader` 更全面的翻译，速度较慢
- 取消了自签名，减小杀毒软件误报的概率

## Version 2.2.0 RPG Maker Utils

- 用换行符`\n`分割文本，显著提升了翻译性能
- 使用 `Bitmap.prototype.drawText` 和 `Bitmap.prototype.measureText`，解决了部分插件中，文本未被翻译的问题（可能会导致宽度计算不准确）
- 尝试删除一些不需要的翻译方法
- 删除了插件里面的注释(code 108)的翻译功能

## Version 2.1.2 RPG Maker Utils

- 更新了自制插件：增加了注释(code 108)的一次性翻译
- 修复了查找密码时，没有前置对话，Null Error的BUG

## Version 2.1.1 RPG Maker Utils

- 自制的翻译插件：`JtJsonTranslationManager.js`
- 增加插件翻译功能
- 增加了回退支持，可以撤销修改（翻译，字体，作弊）
- 增强了翻译方法的性能
- 增加了软件版本显示
- 修复了一些bug

## Version 2.0.0 RPG Maker Utils

- 增加对 RPG Maker MZ 的支持
- 加载 MTools 的翻译 json 文件，并替换翻译：修改 `data/*json` 文件 和 `js/plugins.js` 文件
- 修改字体 （复制系统字体到 www/Fonts，并修改CSS）
- 添加作弊，使用的是 [paramonos/RPG-Maker-MV-MZ-Cheat-UI-Plugin](https://github.com/paramonos/RPG-Maker-MV-MZ-Cheat-UI-Plugin)
- 寻找游戏的宝箱密码
- 文本编码修复

## Version 1.0 寻找礼包码

- WPF 应用
- 根据关键词和密码长度，寻找游戏的礼包码
  
