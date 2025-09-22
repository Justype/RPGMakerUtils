# Version History

## Version 2.3.3 RPG Maker Utils

- 修改了插件备份逻辑，防止被杀毒软件误报
- 修改了`plugins.js`的解析逻辑，使用正则表达式解析

## Version 2.3.2 RPG Maker Utils

- 感谢 @Yricky 提供的修改 #12 ，现在能更好地加入翻译插件了
- 修复了如果`plugins.js`的`parameters`中有键为`name`时，不会被翻译的BUG
- 去除了复制密码到剪贴板的功能

## Version 2.3.1 RPG Maker Utils

- 修复了翻译`plugins.js`时，会把插件名字翻译掉的BUG
- 修复了翻译`108`时，可能会重复翻译的BUG

## Version 2.3.0 RPG Maker Utils

- 替换翻译
  - 新增大范围模式，可以翻译更多的内容（但可能会有误翻译）
	- 翻译 `122` ，只有当 `parameters[3] == 4` 且 `parameters[4]` 是简单的字符串 `'xxxxx'` 并且 值在字典中，才翻译
    - 只要在字典中就翻译 `plugins.js` 的值
    - 只要在字典中就翻译 `356` `357` 的值
  - 优化了翻译方法，提升了翻译性能
  - 对于注释 code `108` 和 `note`使用正则进行翻译 `@"<([^<>:]+):([^<>]*)>"` -> `"<$1:翻译后的内容>"`，避免了误翻译
  - 修复了一些BUG（times参数失效，取消了`101`的翻译等）
  - 使用白名单而不是黑名单翻译`System.json`文件
- 插件翻译
  - 新增 `variables` 和 `switches` 的翻译 （方便作弊使用）
- 找密码
  - 不用 `103` 紧挨着 `111` 了， 只要在5个指令内就行

## Version 2.2.2 RPG Maker Utils

- 去除了重命名文件的功能（防止被Windows Defender报毒）

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
  
