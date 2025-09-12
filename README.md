# RPG Maker MV MZ 帮助软件

本版本使用 .NET Framework 4.8 编写，支持 RPG Maker MV 和 RPG Maker MZ 的游戏。

有可能会被杀毒软件误报为病毒，请自行判断是否下载使用。

## 功能

1. 加载 MTools 的翻译文件 (推荐使用插件进行翻译)
   1. 翻译插件：加载我的翻译插件，并解析json文件，保存到 `www/translations.json`
      1. `Faster` 更快的翻译速度，部分菜单可能未被翻译
      2. `Broader` 更全面的翻译，速度较慢 （推荐，但在JoiPlay里面可能会出现插件菜单未翻译的情况）
   2. 替换翻译：根据json文件，修改 `data/*json` 文件 和 `js/plugins.js` 文件
2. 修改字体 （复制系统字体到 www/Fonts，并修改CSS）
3. 添加作弊，使用的是 [paramonos/RPG-Maker-MV-MZ-Cheat-UI-Plugin](https://github.com/paramonos/RPG-Maker-MV-MZ-Cheat-UI-Plugin) 的代码
4. 找游戏的宝箱密码
5. 自动识别txt文件的编码格式，并转换为 UTF-8

## 使用方法

1. 直接下载 exe 文件
2. 运行 exe 文件
3. 选择游戏目录 和/或 翻译文件(json)
4. 点击修改按钮
5. 等待修改完成

<image src="./assets/1.png" alt="Select Game Directory" width="400"/>

### 直接使用插件

1. 找到游戏的`js/plugins.js`文件，添加我的插件到插件列表中 (最好添加到第一个)
2. 将我的js插件复制到`js/plugins`文件夹中，并重命名为 `JtJsonTranslationManager.js`
3. 将json复制到
   - `MV`: `游戏目录/www/translations.json`
   - `MZ`: `游戏目录/translations.json`
4. 一定要删除 json 里面的`//`“注释”

```js
{"name":"JtJsonTranslationManager","status":true,"description":"Loads translations.json and applies it.","parameters":{}},
```

“注释” 例子 （被GitHub标红）

```json
{
  // 由XXX分享，请尊重劳动成果
  "する": "执行",
}
```

# 原理

## 翻译插件原理

- 对于常用的、只加载一次的字段，直接在游戏加载时替换
- 对于地图事件的对话和选项之类的，替换游戏的渲染方法
- 并且将类命名为`TranslationManager`，有些插件会调用这个类

### Faster 翻译原理

RPG Maker 的相关方法：

- `Window_Message.prototype.startMessage()` 添加对话
- `Window_Command.prototype.addCommand()` 添加选项
- `Window_BattleLog.prototype.addText()` 战斗日志
- `Window_Base.prototype.convertEscapeCharacters()` 会被很多插件调用

### Broader 翻译原理

RPG Maker 的相关方法：

- `Bitmap.prototype.drawText()` 绘制文本
  - `Window_Base.contexts` 就是 `Bitmap` 的实例
  - `Window_Base.prototype.drawText()` 就调用了 `this.contents.drawText()` 也就是 `Bitmap.prototype.drawText()`
- `Bitmap.prototype.measureTextWidth()` 测量文本宽度 (例如计算选项的宽度)
- `Window_Base.prototype.convertEscapeCharacters()` 转义字符 (会被`Window_Base.prototype.drawTextEx()`调用)
  - `Window_Base.prototype.drawTextEx()` 绘制高级文本，是很多插件会调用的方法
  - `Window_Base.prototype.convertEscapeCharacters()` 也会被某些插件调用

总结就是：

1. 只要覆盖了`Bitmap.prototype.drawText()`，`Bitmap.prototype.measureTextWidth()`和`Window_Base.prototype.convertEscapeCharacters()`，大部分的游戏内容和插件都能被翻译
   1. 于此同时，因为覆盖了这么多的方法，提升翻译方法的效率是必要的
   2. 所以要做的几个重要事情是：
       1. 将已经翻译过的内容保存到集合中，以避免重复翻译
       2. 如果遇到字典里没有的内容，由键的长度从长到短依次匹配
       3. 无论是否找到了，都将“译文”保存到集合中，以避免重复翻译或浪费计算资源
       4. 将内容拆分成更小的部分进行翻译（使用`\n`拆分）
2. 又为了方便使用作弊插件，常用的游戏内容（物品、技能、状态、敌人等）都在游戏加载时直接替换

详细内容请查看 RPG Maker MV/MZ 的源码。就在 `js/rpg_objects.js`， `js/rpg_windows.js` 和 `js/rpg_managers.js` 文件中

## 替换翻译原理

[B站视频](https://www.bilibili.com/video/BV1hSJizWEkz/)/[YouTube](https://www.youtube.com/watch?v=W_4BV8pr-iw)： 使用方法与一些代码逻辑

- data/*json 包含了所有的游戏数据
- js/plugins.js 包含了所有的插件配置

只需要翻译 data/*json 文件 和 js/plugins.js 文件即可

需要翻译的字段有：

- `<Object>.json` (`Actors.json`, `Armors.json`, `Classes.json`, `Enemies.json`, `Items.json`, `MapInfos.json`, `Skills.json`, `States.json`, `Weapons.json`)
  - `name`, `description`, `profile` and some `note`
- `events.json` (`MapXXX.json`， `CommonEvents.json`)
  - 对话、背景和选项 `code`: `101`，`401`, `102`，`402`, `405` 翻译所有 `parameters` 中的字符串
  - 插件编码 `code`: `356` 和 `357` 使用白名单进行翻译
- `js/plugins.js`
  - 通过白名单进行翻译

### 插件代码的例子

```json
{
    "code": 356,
    "indent": 1,
    "parameters": ["D_TEXT こんだけ注目集めといてSじゃなかったら・・・ 12"]
}

{
  "code": 357,
  "indent": 0,
  "parameters": [
    "DTextPicture",
    "dText",
    "文字列ピクチャ準備",
    { "text": "ロレンチア\n", "fontSize": "0" }
  ]
}
```

# 鸣谢

1. 感谢 [davide97l/rpgmaker-mv-translator](https://github.com/davide97l/rpgmaker-mv-translator) 提供的思路
2. 感谢 [paramonos/RPG-Maker-MV-MZ-Cheat-UI-Plugin](https://github.com/paramonos/RPG-Maker-MV-MZ-Cheat-UI-Plugin) 提供的作弊代码

