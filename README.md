# RPG Maker MV MZ 帮助软件

本版本使用 .NET Framework 4.8 编写，支持 RPG Maker MV 和 RPG Maker MZ 的游戏。

有可能会被杀毒软件误报为病毒，请自行判断是否下载使用。

## 功能

1. 加载 MTools 的翻译 json 文件，
   1. 翻译插件：加载我的翻译插件，并解析json文件，保存到 `www/translations.json`
   2. 替换翻译：修改 `data/*json` 文件 和 `js/plugins.js` 文件
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

<image src="./assets/1.png" alt="Select Game Directory" width="300"/>

# 原理

## 翻译插件原理

- 对于常用的、只加载一次的字段，直接在游戏加载时替换
- 对于地图事件的对话和选项之类的，替换游戏的渲染方法
- 并且将类命名为`TranslationManager`，有些插件会调用这个类

RPG Maker 的相关方法：

- `Window_Command.prototype.addCommand` 添加选项
- `Window_Message.prototype.startMessage` 对话之类的
- `Window_Base.prototype.convertEscapeCharacters` 转义字符 (会被`Window_Base.prototype.drawTextEx`调用)
  - `Window_Base.prototype.drawTextEx` 是很多插件会调用的方法
  - `Window_Base.prototype.convertEscapeCharacters` 也会被某些插件调用

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

