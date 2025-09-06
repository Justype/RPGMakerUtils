/*:

 * @plugindesc A plugin to translate RPG Maker game text using a JSON dictionary. Supports synchronous and Promise-based translation APIs.
 * @author Justype
 *
 * @help
 * This plugin loads a 'translations.json' file from the 'www' folder and uses it to translate various text strings in the game.
 *
 * Features:
 * - Translates menu commands, item names, skill names, actor names, map names, and more.
 * - Supports both synchronous and Promise-based translation APIs for future-proof code.
 * - Automatically applies translations after database load.
 * - Integrates with event commands and message windows.
 *
 * Usage:
 * 1. Place 'translations.json' in your project's 'www' folder. The file should map original text to translated text.
 *    Example:
 *    {
 *      "Hello": "ÄãºÃ",
 *      "Save": "±£´æ"
 *    }
 * 2. Add this plugin to your project and place it below any plugins that add new menu commands or text.
 * 3. Use TranslationManager in your scripts:
 *    // Synchronous
 *    const translated = TranslationManager.translate("Hello");
 *
 *    // Promise-based (future style)
 *    TranslationManager.getTranslatePromise("Hello").then(translated => {
 *      // Use translated text
 *    });
 *
 *    // For arrays
 *    Promise.all(["A", "B"].map(TranslationManager.getTranslatePromise)).then(arr => { ... });
 *
 * API:
 *   TranslationManager.translate(text)                // Synchronous translation
 *   TranslationManager.getTranslatePromise(text)       // Promise-based translation
 *   TranslationManager.translateIfNeed(text, callback) // Callback style
 *
 * Place this plugin below any plugins that add new menu commands or text.
 *
* @param translationPath
* @text Translation JSON Path
* @desc Path to the translations.json file (relative to www folder). Example: data/translations.json
* @default translations.json
 */

(function() {
    'use strict';
    
    let translationDictionary = null;
    let translationKeysSorted = null;

    // --- Core Logic: Load the Translation Dictionary ---
    // This function is aliased to load our custom data before the game starts.
    const _DataManager_loadDatabase = DataManager.loadDatabase;
    DataManager.loadDatabase = function() {
        _DataManager_loadDatabase.call(this);
        // Get plugin parameters
        var parameters = PluginManager.parameters('JtJsonTranslationManager');
        var translationPath = parameters['translationPath'] || 'translations.json';
        const xhr = new XMLHttpRequest();
        const url = translationPath;
        xhr.open('GET', url, false);
        xhr.overrideMimeType('application/json');
        xhr.onload = function() {
            if (xhr.status < 400) {
                translationDictionary = JSON.parse(xhr.responseText);
                translationDictionary[""] = ""; // Ensure empty string maps to itself
                Object.keys(translationDictionary).forEach(key => {
                    const value = translationDictionary[key];
                    translationDictionary[value] = value; // Ensure translated text maps to itself
                });
                translationKeysSorted = Object.keys(translationDictionary).sort((a, b) => b.length - a.length);
            }
        };
        xhr.onerror = function() {
            throw new Error('Failed to load ' + url);
        };
        xhr.send();
    };

    // Patch DataManager.onLoad to run translation after all database files are loaded
    const _DataManager_onLoad = DataManager.onLoad;
    DataManager._translationApplied = false;
    DataManager.onLoad = function(object) {
        _DataManager_onLoad.call(this, object);
        if (DataManager.isDatabaseLoaded() && !DataManager._translationApplied && translationDictionary) {
            DataManager_translateCommonData();
            DataManager._translationApplied = true;
        }
    };

    const _DataManager_loadMapData = DataManager.loadMapData;
    DataManager.loadMapData = function(mapId) {
        _DataManager_loadMapData.call(this, mapId);
        const _onLoad = DataManager.onLoad;
        DataManager.onLoad = function(object) {
            _onLoad.call(this, object);
            if (object === $dataMap && translationDictionary) {
                DataManager_translateMapData();
            }
        };
    };

    // Static TranslationManager class
    class TranslationManager {
        static translate(text) {
            if (text === null || text === undefined || typeof text !== 'string') {
                return text;
            }
            if (translationDictionary && translationDictionary[text]) {
                return translationDictionary[text];
            }
            // If not in the dictionary, try to find partial matches
            let translatedText = text;
            if (translationDictionary && translationKeysSorted) {
                translationKeysSorted.forEach(key => {
                    translatedText = translatedText.replace(key, translationDictionary[key]);
                });
                // Cache the result for future lookups
                translationDictionary[text] = translatedText;
                translationKeysSorted = Object.keys(translationDictionary).sort((a, b) => b.length - a.length);
            }
            return translatedText;
        }

        // Async-style callback, but runs synchronously
        static translateIfNeed(text, callback) {
            const result = TranslationManager.translate(text);
            if (typeof callback === 'function') {
                callback(result);
            }
            return result;
        }

        // Returns a Promise that resolves with the translated text
        static getTranslatePromise(text) {
            return new Promise((resolve) => {
                const result = TranslationManager.translate(text);
                resolve(result);
            });
        }
    }

    window.TranslationManager = TranslationManager;

    function translateEventCommandText(command) {
        switch (command.code) {
            case 101: // first parameter is the text (character name?)
                command.parameters[0] = TranslationManager.translate(command.parameters[0]);
                break;
            case 401: // Only one parameter which is the text
                command.parameters[0] = TranslationManager.translate(command.parameters[0]);
                break;
            case 102: // The first parameter is a list of choices
                command.parameters[0] = command.parameters[0].map(choice => TranslationManager.translate(choice));
                break;
            case 402: // second parameter is the text of the choices
                command.parameters[1] = TranslationManager.translate(command.parameters[1]);
                break;
            case 405: // Only one parameter (text)
                command.parameters[0] = TranslationManager.translate(command.parameters[0]);
                break;
        }
    }

    function translateEventCommandTextOld(command) {
        if ([101, 401, 102, 402, 405].includes(command.code) && Array.isArray(command.parameters)) {
            command.parameters = command.parameters.map(param => {
                if (typeof param === 'string') {
                    return TranslationManager.translate(param);
                } else if (Array.isArray(param)) {
                    return param.map(p => typeof p === 'string' ? TranslationManager.translate(p) : p);
                }
                return param;
            });
        }
    }

    function DataManager_translateCommonData() {
        // Translate map names in $dataMapInfos
        if ($dataMapInfos) {
            $dataMapInfos.forEach(mapInfo => {
                if (mapInfo && mapInfo.name) {
                    mapInfo.name = TranslationManager.translate(mapInfo.name);
                }
            });
        }

        // Translate Actors' names and profiles
        if ($dataActors) {
            $dataActors.forEach(actor => {
                if (actor && actor.name) actor.name = TranslationManager.translate(actor.name);
                if (actor && actor.profile) actor.profile = TranslationManager.translate(actor.profile);
            });
        }

        // Translate Items, Skills, Weapons, Armors, States
        const dataArrays = [$dataItems, $dataSkills, $dataWeapons, $dataArmors, $dataStates];
        dataArrays.forEach(data => {
            if (data) {
                data.forEach(item => {
                    if (item && item.name) item.name = TranslationManager.translate(item.name);
                    if (item && item.description) item.description = TranslationManager.translate(item.description);
                });
            }
        });

        // Translate Enemies
        if ($dataEnemies) {
            $dataEnemies.forEach(enemy => {
                if (enemy && enemy.name) enemy.name = TranslationManager.translate(enemy.name);
            });
        }

        // Translate the System terms and messages
        if ($dataSystem && $dataSystem.terms) {
            if ($dataSystem.terms.basic) {
                $dataSystem.terms.basic.forEach((term, i) => {
                    $dataSystem.terms.basic[i] = TranslationManager.translate(term);
                });
            }
            if ($dataSystem.terms.commands) {
                $dataSystem.terms.commands.forEach((term, i) => {
                    $dataSystem.terms.commands[i] = TranslationManager.translate(term);
                });
            }
            if ($dataSystem.terms.params) {
                $dataSystem.terms.params.forEach((term, i) => {
                    $dataSystem.terms.params[i] = TranslationManager.translate(term);
                });
            }
            if ($dataSystem.terms.messages) {
                for (let key in $dataSystem.terms.messages) {
                    $dataSystem.terms.messages[key] = TranslationManager.translate($dataSystem.terms.messages[key]);
                }
            }
        }

        // Do not translate event when loaded, otherwise the game will freeze for a long time until all events are translated
        // // Translate event text and choices in common events
        // if ($dataCommonEvents) {
        //     $dataCommonEvents.forEach(event => {
        //         if (event && event.list) {
        //             event.list.forEach(translateEventCommandText);
        //         }
        //     });
        // }
    }

    // Do not call this function. The same issue as common events.
    // To make things worse, everytime you change map, it will be called again.
    function DataManager_translateMapData() {
        // Translate Map display names
        if ($dataMap) {
            if ($dataMap.displayName) {
                $dataMap.displayName = TranslationManager.translate($dataMap.displayName);
            }
            // // Translate event text and choices in the current map
            // if ($dataMap.events) {
            //     $dataMap.events.forEach(event => {
            //         if (event && event.pages) {
            //             event.pages.forEach(page => {
            //                 if (page.list) {
            //                     page.list.forEach(translateEventCommandText);
            //                 }
            //             });
            //         }
            //     });
            // }
        }
    }
    
    // --- Text Display Translation Patches ---
    // Translate all command names in all command windows
    const _Window_Command_addCommand = Window_Command.prototype.addCommand;
    Window_Command.prototype.addCommand = function(name, symbol, enabled = true, ext = null) {
    _Window_Command_addCommand.call(this, TranslationManager.translate(name), symbol, enabled, ext);
    };

    // Translate window texts (like 'Options' or 'Save')
    const _Window_Base_drawText = Window_Base.prototype.drawText;
    Window_Base.prototype.drawText = function(text, x, y, maxWidth, align) {
    text = TranslationManager.translate(text);
        _Window_Base_drawText.call(this, text, x, y, maxWidth, align);
    };

    // Patch Game_Message.setSpeakerName to translate speaker names in event messages
    const _Game_Message_setSpeakerName = Game_Message.prototype.setSpeakerName;
    Game_Message.prototype.setSpeakerName = function(name) {
    _Game_Message_setSpeakerName.call(this, TranslationManager.translate(name));
    };

    // Translate the main message window text
    const _Window_Message_startMessage = Window_Message.prototype.startMessage;
    Window_Message.prototype.startMessage = function() {
        // This is a simple but effective way to translate messages
        // that are set directly in events.
        for (let i = 0; i < $gameMessage._texts.length; i++) {
            $gameMessage._texts[i] = TranslationManager.translate($gameMessage._texts[i]);
        }
        _Window_Message_startMessage.call(this);
    };

    // Translate item/skill names and descriptions
    const _Scene_ItemBase_drawItemName = Scene_ItemBase.prototype.drawItemName;
    Scene_ItemBase.prototype.drawItemName = function(item, x, y, width) {
        if (item) {
            const originalName = item.name;
            item.name = TranslationManager.translate(item.name);
            _Scene_ItemBase_drawItemName.call(this, item, x, y, width);
            item.name = originalName; // Revert the name to prevent permanent changes
        }
    };

    // Patch for the battle log to show translated messages.
    const _Window_BattleLog_addText = Window_BattleLog.prototype.addText;
    Window_BattleLog.prototype.addText = function(text) {
    _Window_BattleLog_addText.call(this, TranslationManager.translate(text));
    };

    // Translate item descriptions in the help window
    const _Window_Help_setItem = Window_Help.prototype.setItem;
    Window_Help.prototype.setItem = function(item) {
        if (item) {
            const originalDescription = item.description;
            item.description = TranslationManager.translate(item.description);
            _Window_Help_setItem.call(this, item);
            item.description = originalDescription; // Revert the description
        } else {
            _Window_Help_setItem.call(this, item);
        }
    };

    // // --- Plugin Command Argument Translation ---
    // var PLUGIN_WHITE_DICT = {
    //     "D_TEXT": null,
    //     "DTextPicture": ["text"],
    // };
    // // Override plugin command handling to translate arguments
    // regexNonEnglishNumbersSpace = /^[a-zA-Z0-9 _-]+$/;

    // // Use different methods for MV and MZ
    // switch (Utils.RPGMAKER_NAME) {
    //     case "MV":
    //         Game_Interpreter.prototype.command356 = function() {
    //             var args = this._params[0].split(" ");
    //             const command = args.shift();
    //             if (Object.keys(PLUGIN_WHITE_DICT).includes(command)) {
    //                 // Translate arguments that contain non-English characters
    //                 for (let i = 1; i < args.length; i++) {
    //                     if (!args[i].match(regexNonEnglishNumbersSpace)) {
    //                         args[i] = TranslationManager.translate(args[i]);
    //                     }
    //                 }
    //             }
    //             this.pluginCommand(command, args);
    //             return true;
    //         };
    //         break;
    //     case "MZ":
    //         Game_Interpreter.prototype.command356 = function(params) {
    //             var args = params[0].split(" ");
    //             const command = args.shift();

    //             if (Object.keys(PLUGIN_WHITE_DICT).includes(command)) {
    //                 // Translate arguments that contain non-English characters
    //                 for (let i = 1; i < args.length; i++) {
    //                     if (!args[i].match(regexNonEnglishNumbersSpace)) {
    //                         args[i] = TranslationManager.translate(args[i]);
    //                     }
    //                 }
    //             }
    //             this.pluginCommand(command, args);
    //             return true;
    //         };
    //         break;
    // }

    // function recursiveTranslate(obj, safeKeys = null, isSafe = false) {
    //     if (typeof obj === 'string') {
    //         if (isSafe)
    //             return getTranslation(obj);
    //         return obj;
    //     } else if (Array.isArray(obj)) {
    //         return obj.map(item => recursiveTranslate(item));
    //     } else if (typeof obj === 'object' && obj !== null) {
    //         for (let key in obj) {
    //             if (safeKeys) {
    //                 isSafe = safeKeys.includes(key);
    //                 recursiveTranslate(obj[key], safeKeys, isSafe);
    //             } else {
    //                 obj[key] = recursiveTranslate(obj[key]);
    //             }
    //         }
    //         return obj;
    //     }
    //     return obj;
    // }

    // // 357 are only in MZ
    // Game_Interpreter.prototype.command357 = function(params) {
    //     const pluginName = Utils.extractFileName(params[0]);
    //     // If the plugin is in the whitelist, translate its arguments
    //     if (PLUGIN_WHITE_DICT[pluginName]) {
    //         args = recursiveTranslate(params[3], safeKeys = PLUGIN_WHITE_DICT[pluginName]);
    //     }
    //     PluginManager.callCommand(this, pluginName, params[1], args);
    //     return true;
    // };

})();
