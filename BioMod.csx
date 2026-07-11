// Script by BioTomateDE for UndertaleModTool v0.9.0.0
// SPDX-License-Identifier: GPL-3.0-only

// NOTE: Still in early development. Use with caution.

using static UndertaleModLib.Models.UndertaleGameObject;
using static UndertaleModLib.Models.EventSubtypeKey;

// ==================== CONFIG ====================
double BG_OPACITY = 0.6;        // 0.0 - 1.0
double UI_SCALE = 1.0;          // 0.4 - 2.0
int RAINBOWNESS = 150;          // 0 - 255
int RAINBOW_SPEED = 30;         // 1 - 200
int SPEEDHACK_SLOW_FPS = 10;    // 1 - 30
int SPEEDHACK_FAST_FPS = 150;   // 30 - 1000

// These keys can be any EventSubtypeKey.
// https://github.com/UnderminersTeam/UndertaleModTool/blob/stable-0.9/UndertaleModLib/Models/UndertaleGameObject.cs?plain=1#L793-L1165
var HELP_KEY = vk_f1;           // Shows keybinds and what they do
var SHOW_INFO_KEY = I;          // Shows miscellaneous info in the top-left
var SHOW_HITBOXES_KEY = H;      // Shows bounding boxes of Frisk, Solids, Interactables, Event Triggers
var NOCLIP_KEY = N;             // Makes you able to walk through walls and objects
var MOVE_FREE_KEY = M;          // Makes you able to walk during cutscenes/dialogue and allows you to open your menu
var PREVIOUS_ROOM_KEY = J;      // Jumps to the previous room
var NEXT_ROOM_KEY = K;          // Jumps to the next room
var MASH_TEXT_KEY = V;          // Skips dialogue as fast as possible when held down
var ROOM_SWITCHER_KEY = R;      // Opens an interface where you can type in room IDs/names to teleport to
var SPEEDHACK_SLOW_KEY = G;     // Decreases the game speed to SPEEDHACK_SLOW_FPS frames per second
var SPEEDHACK_FAST_KEY = F;     // Increases the game speed to SPEEDHACK_FAST_FPS frames per second
var GET_PUNCHCARD_KEY = P;      // Gives you a punchcard in the first inventory slot
var BATTLE_CANCEL_KEY = B;      // Stops an encounter or battle immediately
// ================================================

const string MOD_VERSION = "0";
const string MAGIC = "$$biomod";
const string WATERMARK = MAGIC + MOD_VERSION;

EnsureDataLoaded();

CodeImportGroup importGroup = new(Data)
{
    ThrowOnNoOpFindReplace = true,
    MainThreadAction = MainThreadAction,
};

List<(string, string)> Macros = new();
MakeMacro("MOD_VERSION", '"' + MOD_VERSION + '"');

MakeMacro("BG_OPACITY", BG_OPACITY, 0.0, 1.0);

MakeMacro("UI_SCALE", UI_SCALE, 0.4, 2.0);
MakeMacro("SCREEN_WIDTH", $"(view_wport[0] / {UI_SCALE})");
MakeMacro("SCREEN_HEIGHT", $"(view_hport[0] / {UI_SCALE})");

MakeMacro("RAINBOWNESS", RAINBOWNESS, 0, 255);
MakeMacro("RAINBOW_SPEED", RAINBOW_SPEED, 1, 200);

MakeMacro("SPEEDHACK_SLOW_FPS", SPEEDHACK_SLOW_FPS, 1, 30);
MakeMacro("SPEEDHACK_FAST_FPS", SPEEDHACK_FAST_FPS, 30, 1000);

MakeKeyMacro("HELP", HELP_KEY);
MakeKeyMacro("INFO", SHOW_INFO_KEY);
MakeKeyMacro("HITBOXES", SHOW_HITBOXES_KEY);
MakeKeyMacro("NOCLIP", NOCLIP_KEY);
MakeKeyMacro("FREEMOVE", MOVE_FREE_KEY);
MakeKeyMacro("ROOM_PREV", PREVIOUS_ROOM_KEY);
MakeKeyMacro("ROOM_NEXT", NEXT_ROOM_KEY);
MakeKeyMacro("ROOMSWITCHER", ROOM_SWITCHER_KEY);
MakeKeyMacro("MASH", MASH_TEXT_KEY);
MakeKeyMacro("SPEEDHACK_SLOW", SPEEDHACK_SLOW_KEY);
MakeKeyMacro("SPEEDHACK_FAST", SPEEDHACK_FAST_KEY);
MakeKeyMacro("PUNCHCARD", GET_PUNCHCARD_KEY);
MakeKeyMacro("BATTLECANCEL", BATTLE_CANCEL_KEY);

Dictionary<string, (int, UndertaleGameObject)> ObjectMap = new();
MapObjects();

if (CheckAlreadyPatched()) return;
UndertaleString watermark = Data.Strings.MakeString(WATERMARK + "_BROKEN");

RegisterHotkey(SHOW_HITBOXES_KEY, "global._hitbox = !global._hitbox;", true);
RegisterHotkey(NOCLIP_KEY, "global._noclip = !global._noclip;", true);
RegisterHotkey(MOVE_FREE_KEY, @"
global.interact = 0;
global.flag[17] = 0;
global._movefree = true;
", false, true);
RegisterHotkey(PREVIOUS_ROOM_KEY, "room_goto_previous(); global.entrance = 0; global.interact = 0;");
RegisterHotkey(NEXT_ROOM_KEY, "room_goto_next(); global.entrance = 0; global.interact = 0;");
ImplRoomSwitcher();

RegisterHotkey(MASH_TEXT_KEY, @"
global._mashing = true;
with (OBJ_WRITER) {
    if (halt) {
        event_user(0);
    } else if (instance_exists(obj_dialoguer)) {
        stringpos = string_length(originalstring);
    }
}
", true, true);

RegisterHotkey(BATTLE_CANCEL_KEY, @"
if (room == room_battle) {
    if (global.flag[15] == 0) {
        audio_stop_sound(global.batmusic);
    }
    room_goto(global.currentroom);
    exit;
}
with (obj_battler) {
    global.interact = 0;
    instance_destroy();
    audio_resume_sound(global.currentsong);
    audio_sound_gain(global.currentsong, 1);
}
with (obj_battleblcon) {
    global.interact = 0;
    instance_destroy();
}
with (obj_tempblack) {
    instance_destroy();
}
with (obj_transheart) {
    instance_destroy();
}
", true);

PrependCode(Data.Scripts.ByName("scr_textskip").Code, @"
if (global._mashing) {
    with (OBJ_WRITER) {
        stringpos = string_length(originalstring);
        event_user(0);
    }
    exit;
}
");

RegisterHotkey(GET_PUNCHCARD_KEY, @"
if (global.interact) exit;

var punchcount = -1;
do {
    scr_itemremove(26);
    punchcount++;
} until (!removed);

if (global.item[7]) {
    global.msg[0] = ""* You don't have enough space!/%%"";
    scr_regulartext();
    exit;
}

for (var i = 7; i > 0; i--) {
    global.item[i] = global.item[i-1];
}
global.item[0] = 26;
if (punchcount < 1) {
    global.msg[0] = '* Have fun ;3/%%';
} else {
    global.msg[0] = ""* What^1? No^1, you can't get a&  second punch card!/%%"";
}
// TODO: you can move through this.
scr_regulartext();
");

RegisterHotkey(SHOW_INFO_KEY, "if (++global._info > 2) global._info = 0;", true);
RegisterHotkey(HELP_KEY, "global._help = !global._help; global._thegame = obj_time.time % 66 == 0", true);
ReplaceCode(EventHandler("obj_time", EventSubtypeOther.RoomStart), "room_speed = global._speedhack;");

RegisterHotkey(SPEEDHACK_FAST_KEY, @"
if (global._speedhack == SPEEDHACK_FAST_FPS) {
    global._speedhack = 30;
    room_speed = 30;
} else {
    global._speedhack = SPEEDHACK_FAST_FPS;
    room_speed = SPEEDHACK_FAST_FPS;
}
", true);

RegisterHotkey(SPEEDHACK_SLOW_KEY, @"
if (global._speedhack == SPEEDHACK_SLOW_FPS) {
    global._speedhack = 30;
    room_speed = 30;
} else {
    global._speedhack = SPEEDHACK_SLOW_FPS;
    room_speed = SPEEDHACK_SLOW_FPS;
}
", true);

RegisterHotkey(ROOM_SWITCHER_KEY, @"
instance_create(0, 0, obj_roomswitcher);
global._help = false;
", true);

ReplaceCode(EventHandler("obj_time", EventSubtypeDraw.DrawGUIEnd), @"
draw_set_halign(fa_left);
draw_set_font(fnt_maintext);

var x1 = SCREEN_WIDTH * 0.2;
var y1 = SCREEN_HEIGHT * 0.25;
var x2 = SCREEN_WIDTH * 0.8;
var y2 = SCREEN_HEIGHT * 0.75;

if (instance_exists(obj_roomswitcher)) {
    draw_set_color(c_black);
    draw_set_alpha(BG_OPACITY);
    draw_rectangle(x1-10, y1-10, x2+10, y2+10, false);
    draw_set_alpha(1);

    draw_set_font(fnt_main);
    draw_text_rainbow(x1, y1-6, 'Room Switcher');

    draw_set_font(fnt_maintext);
    draw_set_color(c_gray);
    draw_text(x1, y2-55, 'Press Enter to confirm / close');
    draw_text(x1, y2-40, 'Press Tab to autocomplete');
    draw_text(x1, y2-25, 'The room_ prefix is not needed');
    draw_text(x1, y2-10, 'You can use Ctrl+Backspace, Ctrl+Left and Ctrl+Right');

    var inputx = x1;
    var inputy = y1 + 30;
    var txt = obj_roomswitcher.text;
    if (txt == '') {
        draw_set_color(c_silver);
        txt = 'Enter Room Name or ID';
    }
    else if (scr_parse_room(txt)) {
        draw_set_color(c_yellow);
    }
    else {
        var candidate = scr_complete_roomname(txt);
        if (candidate != '') {
            draw_set_color(c_gray);
            draw_text(inputx, inputy, '> ' + candidate);
        }
        draw_set_color(c_white);
    }
    draw_text(inputx, inputy, '> ' + txt);

    var cursor_pos = string_width(string_copy(txt, 1, obj_roomswitcher.cursor)) + string_width('> ');
    draw_set_alpha(0.8);
    draw_set_color(c_aqua);
    draw_rectangle(inputx+cursor_pos, inputy+4, inputx+cursor_pos, inputy+2+font_get_size(fnt_maintext), false);
    draw_set_alpha(1);
}

// Draw help
if (global._help) {
    draw_set_color(c_black);
    draw_set_alpha(BG_OPACITY);
    draw_rectangle(x1-10, y1-10, x2+10, y2+10, false);
    draw_set_alpha(1);

    if (global._thegame) {
        draw_sprite(spr_skatebone, 0, x2 -  48, y1 - 1);
    }

    draw_set_font(fnt_main);
    var eks = draw_text_rainbow(x1, y1-6, 'BioMod');
    draw_set_font(fnt_maintext);

    draw_set_color(c_white);
    draw_text(eks+8, y1+6, 'v' + MOD_VERSION);

    draw_set_font(fnt_maintext);
    draw_set_color(c_white);
    xx = x1;
    yy = y1 + 28;

    scr_draw_help_row(KEY_HELP, 'Shows this help lol');
    scr_draw_help_row(KEY_ROOMSWITCHER, 'Opens an interface to teleport any room');
    scr_draw_help_row(KEY_ROOM_PREV, 'Teleports you to the previous room');
    scr_draw_help_row(KEY_ROOM_NEXT, 'Teleports you to the next room');
    scr_draw_help_row(KEY_INFO, 'Shows miscellaneous info in the top left');
    scr_draw_help_row(KEY_HITBOXES, 'Shows hitboxes of Frisk, walls, interactables and events');
    scr_draw_help_row(KEY_NOCLIP, 'Allows you to move through walls and objects');
    scr_draw_help_row(KEY_FREEMOVE, 'Allows you to move during cutscenes and enables the menu');
    scr_draw_help_row(KEY_SPEEDHACK_SLOW, 'Slows the game down to ' + string(SPEEDHACK_SLOW_FPS) + ' FPS');
    scr_draw_help_row(KEY_SPEEDHACK_FAST, 'Speeds the game up to ' + string(SPEEDHACK_FAST_FPS) + ' FPS');
    scr_draw_help_row(KEY_MASH, 'Skips dialogue as fast as possible while held');
    scr_draw_help_row(KEY_PUNCHCARD, 'Gives you a punchcard (needs menu and dialogue closed)');
    scr_draw_help_row(KEY_BATTLECANCEL, 'Cancels the current encounter/battle');
}

// Draw active modules in rainbow color in the top-right corner
row = 0;
if (global._speedhack != 30) {
    scr_draw_module_text('Speedhack ' + string(global._speedhack / 30) + 'x');
}
if (global._hitbox) {
    scr_draw_module_text('Hitboxes');
}
if (global._noclip) {
    scr_draw_module_text('Noclip');
}
if (global._mashing) {
    scr_draw_module_text('Mashing text');
}
if (global._movefree) {
    scr_draw_module_text('Freeing movement');
}

// These variables are set in Keyboard events which are triggered every frame the key is held.
// This makes sure this variables doesn't stay true forever without a KeyRelease event.
global._mashing = false;
global._movefree = false;

// Draw Misc Info in the top-left corner
if (!global._info) exit;
var verbose = global._info == 2;
draw_set_color(c_white);
row = 0;
if (room == room_battle) {
    draw_text_row('Battle Group: ' + string(global.battlegroup) + ' (' + scr_battlegroup_name(global.battlegroup) + ')');

    var enemyhp = 'Enemy HP: ' + string(global.monsterhp[0]);
    if (global.monster[1] || global.monster[2]) {
        enemyhp += ', ' + string(global.monsterhp[1]);
    }
    if (global.monster[2]) {
        enemyhp += ', ' + string(global.monsterhp[2]);
    }
    draw_text_row(enemyhp);

    draw_text_row('Turn: ' + string(global.turn));
    if (verbose) {
        draw_text_row('X: ' + string(obj_heart.x) + ', Y: ' + string(obj_heart.y));
    }
    if (global.mercy == 0) {
        var text = 'Flee Chance: ' + string(scr_fleechance()) + '%';
        if (verbose && reason != '') {  // set by scr_fleechance
            text += ' (' + reason + ')';
        }
        draw_text_row(text);
    }
    if (global.myfight == 0 && (global.bmenuno == 3 || global.bmenuno == 3.5)) {
        var itempos = global.bmenucoord[3] + ((global.bmenuno - 3) * 8);
        draw_text_row('Item Effect: ' + scr_itemstat(global.item[itempos]));
    }
    if (instance_exists(obj_targetchoice) || instance_exists(obj_targetchoicefist) || instance_exists(obj_shoetargettest)) {
        var mindmg = scr_attackdamage(false);
        var maxdmg = scr_attackdamage(true);
        var avgdmg = round((mindmg + maxdmg) / 2);
        draw_text_row('Attack Damage: ' + string(mindmg) + '-' + string(maxdmg) + ' (Avg: ' + string(avgdmg) + ')');
    }
    if (verbose && global.invc) {
        draw_text_row('Invincibility: ' + string(global.invc));
    }
} else {
    if (verbose) {
        draw_text_row('Frame: ' + string(time));
        draw_text_row('Entrance: ' + string(global.entrance));
        if (instance_exists(obj_mainchara)) {
            draw_text_row('X: ' + string(obj_mainchara.x) + ', Y: ' + string(obj_mainchara.y));
        } else {
            draw_text_row('X: ?, Y: ?');
        }
    }
    draw_text_row('Room: ' + string(room) + ' (' + room_get_name(room) + ')');
    draw_text_row('Interact: ' + string(global.interact));
    draw_text_row('Plot: ' + string(global.plot));
    if (instance_exists(obj_encountererparent)) {
        if (verbose) {
            draw_text_row('Encounterer: ' + object_get_name(obj_encountererparent.object_index));
        }
        draw_text_row('Steps: ' + string(global.encounter) + '/' + string(floor(obj_encountererparent.steps)));
        draw_text_row('Steps RNG: ' + string(global._stepmin) + '-' + string(global._stepmax) + ' (Avg: ' + string(floor((global._stepmax + global._stepmin) / 2)) + ')');
    }
    if (global.battlegroup) {
        draw_text_row('Battle Group: ' + string(global.battlegroup) + ' (' + scr_battlegroup_name(global.battlegroup) + ')');
    }
    if (global.phasing) {
        draw_text_row('Phasing');
    }
    if (global.flag[17]) {
        draw_text_row('Menu Disabled');
    }
    // this prevents dozens of rows for the Three Switch Room Skip lol
    if (!instance_exists(obj_triswitchevent)) {
        with (OBJ_WRITER) {
            row = other.row;
            draw_text_row('Textboxes: ' + string(stringno+1) + '/' + string(scr_textbox_count(self)));
            other.row = row;
        }
    }
    if (verbose) {
        var doortime = scr_doortimer();
        if (doortime) {
            draw_text_row('Door Timer: ' + string(doortime));
        }
    }
}
");

ReplaceCode(EventHandler("obj_mainchara", EventSubtypeDraw.DrawEnd), @"
if (!global._hitbox) exit;

// Draw default/wrongwarp position.
var xs = xstart;
var ys = ystart;
// this garbage logic is taken from gml_Object_obj_mainchara_Create_0
if (xs % 3 == 2) xs++;
if (xs % 3 == 1) xs--;
if (ys % 3 == 2) ys++;
if (ys % 3 == 1) ys--;
draw_set_color(c_orange);
draw_rectangle(xs, ys+19, xs+19, ys+29, true);

// Draw interact seeking box.
// (Coords taken from gml_Object_obj_mainchara_Other_10)
draw_set_color(c_red);
if (global.facing == 1 /* Right */) {
    draw_rectangle(x+(sprite_width/2), y+19, x+sprite_width+15, y+sprite_height, true);
} else if (global.facing == 3 /* Left */) {
    draw_rectangle(x+(sprite_width/2), y+19, x-15, y+sprite_height, true);
} else if (global.facing == 0 /* Down */) {
    draw_rectangle(x+4, y+20, x+sprite_width-4, y+sprite_height+15, true);
} else if (global.facing == 2 /* Up */) {
    draw_rectangle(x+4, y+sprite_height-5, x+sprite_width-4, y+8, true);
}

// Draw bounding box for next step.
draw_set_color(c_olive);
draw_set_alpha(0.6);
draw_rectangle(bbox_left-3, bbox_top-3, bbox_right+3, bbox_bottom+3, true);
draw_set_alpha(1);

// Draw bounding box.
draw_set_color(c_yellow);
draw_rectangle(bbox_left, bbox_top, bbox_right, bbox_bottom, true);
");

ReplaceCode(EventHandler("obj_heart", EventSubtypeDraw.DrawEnd), @"
if (!global._hitbox) exit;
draw_set_color(sprite_index == spr_heartyellow_flip ? c_red : c_yellow);
draw_rectangle(bbox_left, bbox_top, bbox_right, bbox_bottom, true);
");

ImplNoclip();

List<UndertaleGameObject> doors = new();
foreach (var obj in Data.GameObjects)
{
    if (IsParent(obj, "obj_interactable"))
    {
        if (!obj.Visible) SuppressRendering(obj);
        obj.Visible = true;
    }

    if (!IsVisible(obj) && FindFriskCollision(obj) is UndertaleCode code)
    {
        // Exclude empty events with just an Exit instruction.
        if (code.Instructions.Count > 1)
        {
            ShowEvent(obj.Name.Content);
            obj.Visible = true;
        }
    }

    if (!obj.Visible && IsParent(obj, "obj_doorparent"))
    {
        doors.Add(obj);
        SuppressRendering(obj);
        obj.Visible = true;
    }

    // These objects are solid anyway due to mainchara collison code.
    // This fixes noclip for these objects.
    if (IsParent(obj, "obj_solidnpcparent"))
    {
        obj.Solid = false;
    }

    if (IsParent(obj, "obj_solidparent"))
    {
        obj.Solid = false;
        if (obj.Visible)
        {
            ReplaceCode(EventHandler(obj, EventSubtypeDraw.Draw), "draw_self(); event_inherited();");
        }
        obj.Visible = true;
    }

    if (obj.Name.Content.StartsWith("obj_marker"))
    {
        obj.Visible = true;
        ReplaceCode(EventHandler(obj, EventSubtypeDraw.Draw), @"
if (global._hitbox) {
    draw_set_color(c_maroon);
    draw_rectangle(bbox_left, bbox_top+19, bbox_left+19, bbox_top+29, true);
}
        ");
    }

    // TODO: fix noclip for these fuckass objects
    //if (obj.Solid) Console.WriteLine($"{obj} still solid, parent is {obj.ParentId}");

    // This fixes invisibility issues because obj_solidsmall
    // will be patched to have an empty Draw event.
    if (obj.ParentId == GetObject("obj_solidsmall"))
    {
        obj.ParentId = GetObject("obj_solidparent");
    }
}

ReplaceCode(EventHandler("obj_interactable", EventSubtypeDraw.DrawEnd), @"
if (global._hitbox) {
    draw_set_color(c_green);
    draw_rectangle(bbox_left, bbox_top, bbox_right, bbox_bottom, true);
}
");

ReplaceCode(EventHandler("obj_solidnpcparent", EventSubtypeDraw.DrawEnd), @"
if (global._hitbox) {
    draw_set_color(c_lime);
    draw_rectangle(bbox_left, bbox_top, bbox_right, bbox_bottom, true);
}
");

ReplaceCode(EventHandler("obj_doorparent", EventSubtypeDraw.DrawEnd), @"
if (global._hitbox) {
    draw_set_color(c_fuchsia);
    draw_rectangle(bbox_left, bbox_top, bbox_right, bbox_bottom, true);
}
");
SuppressRendering("obj_doorparent");

ReplaceCode(EventHandler("obj_solidparent", EventSubtypeDraw.Draw), @"
if (global._hitbox) {
    draw_set_color(c_blue);
    draw_set_alpha(0.5);
    draw_rectangle(bbox_left, bbox_top, bbox_right, bbox_bottom, false);
    draw_set_alpha(1);
}
");

// TODO: are these still neeeded?
ShowEvent("obj_bouncetileparent", true);
GetObject("obj_holedown2").Visible = true;
GetObject("obj_holedown3").Visible = true;

GetObject("obj_iceevent").Visible = false;
GetObject("obj_inwaterspot").Visible = false;
ShowEvent("obj_teleigloo", true);
PrependCode(EventHandler("obj_dogetrigger1", EventSubtypeDraw.Draw), "if (sprite_index == spr_event) exit;");
PrependCode(EventHandler("obj_dogringbell", EventSubtypeDraw.Draw), "if (sprite_index == spr_event) exit;");

string[] slopes = ["obj_sul", "obj_sur", "obj_sdl", "obj_sdr"];
foreach (var objName in slopes)
{
    var obj = GetObject(objName);
    obj.Visible = true;
    ReplaceCode(EventHandler(obj, EventSubtypeStep.Step), "visible = global._hitbox");
}

foreach (var ev in GetObject("obj_heart").Events[(int)EventType.Collision])
{
    // TODO: does this mess up sans shaker
    PrependCode(ev.Actions[0].CodeId, "if (global._noclip) exit;");
}

FindReplaceCode(Data.Scripts.ByName("SCR_GAMESTART").Code, "global.battlegroup = 3;", "global.battlegroup = 0;");

AppendCode(EventHandler("obj_time", EventSubtypeOther.GameStart), @"
display_set_gui_size(SCREEN_WIDTH, SCREEN_HEIGHT);

global._thegame = 0;
global._help = false;
global._info = false;
global._hitbox = false;
global._noclip = false;
global._mashing = false;
global._movefree = false;

global._speedhack = 30;
global._stepmin = 0;
global._stepmax = 0;
");

MakeScript("scr_battlegroup_name", BattlegroupCode);

MakeScript("draw_text_row", @"
var yoff = (row == 0) ? -2 : 0;
var xx = 5;
var yy = 5 + 15 * (row++);

var color = draw_get_color();
var alpha = draw_get_alpha();

draw_set_color(c_black);
draw_set_alpha(BG_OPACITY);
draw_rectangle(xx-3, yy+yoff+2, xx+string_width(argument0)+2, yy+16, false);

draw_set_color(color);
draw_set_alpha(alpha);
draw_text(xx, yy, argument0);
");

MakeScript("draw_text_rainbow", @"
var xx = argument0;
var yy = argument1;
var strlen = string_length(argument2);
var oldcolor = draw_get_color();
for (var i = 1; i <= strlen; i++) {
    var char = string_char_at(argument2, i);
    var hue = (time / global._speedhack * RAINBOW_SPEED) + xx + yy;
    draw_set_color(make_color_hsv(hue % 255, RAINBOWNESS, 255));
    draw_text(xx, yy, char);
    xx += string_width(char);
}
draw_set_color(oldcolor);
return xx;
");

MakeScript("scr_draw_module_text", @"
var yoff = (row == 0) ? -2 : 0;
var w = string_width(argument0);
var xx = SCREEN_WIDTH - w - 5;
var yy = 5 + 15 * (row++);

var color = draw_get_color();
var alpha = draw_get_alpha();

draw_set_color(c_black);
draw_set_alpha(BG_OPACITY);
draw_rectangle(xx-3, yy+yoff+2, xx+w+2, yy+16, false);

draw_set_color(color);
draw_set_alpha(alpha);
draw_text_rainbow(xx, yy, argument0);
");

MakeScript("scr_draw_help_row", @"
draw_text_rainbow(xx, yy, argument0);
draw_text(xx + 25, yy, argument1);
yy += 15;
");

// TODO: return an array for ballet shoes/burnt pan
MakeScript("scr_attackdamage", @"
var variance = argument0 ? 2 : 0;

if (instance_exists(obj_shoetargettest)) {
    var damage = global.wstrength + global.at - global.monsterdef[global.mytarget];
    damage *= ((obj_shoetargettest.myscore / 160) * (4 / obj_shoetargettest.num));
    return round(damage) + variance;
}

var damage = global.wstrength + global.at - global.monsterdef[global.mytarget] + variance;
var mult, myx;
if (instance_exists(obj_targetchoice)) {
    myx = obj_targetchoice.x + (obj_targetchoice.sprite_width / 2);
    mult = 2.2;
} else {
    myx = obj_targetchoicefist.x + (obj_targetchoicefist.sprite_width / 2);
    mult = 2.1;
}

var bonusfactor = abs(myx - obj_target.x - (obj_target.sprite_width / 2));
if (bonusfactor == 0) {
    bonusfactor = 1;
}

if (bonusfactor > 12) {
    return round(damage * 2 * (obj_target.sprite_width - bonusfactor) / obj_target.sprite_width);
} else {
    return round(damage * mult);
}
");

MakeScript("scr_fleechance", @"
// Logic taken from gml_Script_scr_runaway
reason = '';
if (instance_exists(obj_undyneboss)) {
    reason = 'Fighting Undyne';
    return 100;
}
if (global.armor == 4 && !global.flag[6]) {
    reason = 'Wearing Bandage';
    return 100;
}
if (global.flag[6] && global.turn < 1) {
    reason = 'Turn 1 in Hard Mode';
    return 0;
}

// deep magic
return min(100, 50 + 10 * (global.turn - global.flag[6]));
");

MakeScript("scr_parse_room", @"
var idx = real(argument0);
if (idx && room_exists(idx)) {
    return idx;
}
idx = asset_get_index(argument0);
if (room_exists(idx)) {
    return idx;
}
idx = asset_get_index('room_' + argument0);
if (room_exists(idx)) {
    return idx;
}
");

{
    string src = "";
    foreach (var door in doors)
    {
        src += "with (" + door.Name.Content + ") { if (touched) { return alarm[2]; }}";
    }
    src += "return -1";
    MakeScript("scr_doortimer", src);
}

MakeScript("scr_textbox_count", @"
var len = array_length_1d(argument0.mystring);
for (var i = 0; i < len; i++) {
    var s = argument0.mystring[i];
    if (string_pos('%%', s) || string_pos('\C', s)) {
        return i + 1;
    }
}
// hopefully never hit lol
");

MakeScript("scr_itemstat", @"
// Info from gml_Script_scr_itemuseb
switch (argument0) {
    case 1: return '+10 HP';  // Monster Candy
    case 2: return '+15 HP';  // Croquet Roll
    case 3: // Stick
        if (instance_exists(obj_dogeparent) || instance_exists(obj_wizard)) {
            return 'Mercy';
        }
        with (obj_ratingsmaster) {
            return typeuse[7] ? '+1 Rating' : '+700 Rating';
        }
        return '---';
    case 4: return '+10 HP';  // Bandage
    case 5: return '+1 HP';  // Rock Candy
    case 6: return '+8 HP';  // Pumpkin Rings
    case 7: return '+12 HP';  // Spider Donut
    case 8: return '+5 HP';  // Stoic Onion
    case 9: return '+16 HP';  // Ghost Fruit
    case 10: return '+24 HP';  // Spider Cider
    case 11:  // Butterscotch Pie
        if (instance_exists(obj_asgoreb)) {
            return 'Max HP, -1 EnemyDF, -5 EnemyAT';
        }
        if (instance_exists(obj_ripoff_toriel)) {
            return 'Max HP, +3 Mercy';
        }
        return 'Max HP';
    case 12: return '3 DF';  // Faded Ribbon
    case 13: return '3 AT';  // Toy Knife
    case 14: return '5 AT';  // Tough Glove
    case 15: return '7 DF';  // Manly Bandanna
    case 16: return '+45 HP';  // Snowman Piece
    case 17: return '+15 HP';  // Nice Cream
    case 18: return '+28 HP';  // Puppydough Icecream
    case 19: return '+11 HP';  // Bisicle
    case 20: return '+11 HP';  // Unisicle
    case 21: return '+22 HP';  // Cinnamon Bun
    case 22: return '+2 HP';  // Temmie Flakes
    case 23: return '+34 HP';  // Abandoned Quiche
    case 24: return '10 DF';  // Old Tutu
    case 25: return '7 AT';  // Ballet Shoes
    case 26:  // Punch Card
        if (global.weapon == 14) {
            if (global.at > 28) return '+2 AT';
            if (global.at > 26) return '+3 AT';
            if (global.at > 23) return '+4 AT';
            if (global.at > 18) return '+5 AT';
            return '+6 AT';
        }
        return '---';
    case 27: return '';  // Annoying Dog
    case 28: return '+2/10/30/999 HP';  // Dog Salad
    case 29: case 30: case 31: case 32: case 33: case 34: return 'Bloat Inventory';  // Dog Residue
    case 35: return '+21 HP';  // Astronaut Food
    case 36:  // Instant Noodles
        if (global.inbattle) {
            if (global.seriousbattle) {
                return '+90 HP';
            }
            return '+4 HP';
        }
        return '+15 HP';
    case 37: return '+18 HP';  // Crab Apple
    case 38: return '+20 HP';  // Hot Dog
    case 39: return '+21 HP';  // Hot Cat
    case 40:  // Glamburger
        if (instance_exists(obj_ratingsmaster)) {
            return typeuse[10] ? '+27 HP, +300 Rating' : '+27 HP, +500 Rating';
        }
        return '+27 HP';
    case 41:  // Sea Tea
        if (global.inbattle && global.sp < 8 && !instance_exists(obj_spiderb)) {
            return '+10 HP, +1 SP';
        }
        return '+10 HP';
    case 42:  // Starfait
        if (instance_exists(obj_ratingsmaster)) {
            return typeuse[9] ? '+14 HP, +200 Rating' : '+14 HP, +300 Rating';
        }
        return '+14 HP';
    case 43:  // Legendary Hero
        var stat = '+40 HP';
        if (global.inbattle && global.at < 150) {
            stat += ', +4 AT';
        }
        if (instance_exists(obj_ratingsmaster)) {
            stat += typeuse[9] ? ', 200 Rating' : ', 300 Rating';
        }
        return stat;
    case 44: return '5 DF';  // Cloudy Glasses
    case 45: return '2 AT';  // Torn Notebook
    case 46: return '11 DF';  // Stained Apron
    case 47: return '10 AT';  // Burnt Pan
    case 48: return '12 DF, +5 AT';  // Cowboy Hat
    case 49: return '12 AT';  // Empty Gun
    case 50: return '15 DF';  // Heart Locket
    case 51: return '15 AT';  // Worn Dagger
    case 52: return '99 AT';  // Real Knife
    case 53: return '99 DF';  // The Locket
    case 54: return global.hp <= 3 ? 'Max HP' : '-1 HP';  // Bad Memory
    case 55: return '+17 HP'; // Dream
    case 56: return '---'; // Undyne's Letter
    case 57: return '---'; // Undyne Letter EX
    case 58: return '+13 HP';  // Popato Chisps
    case 59:  // Junk Food
        if (instance_exists(obj_ratings_master)) {
            return '+17 HP, -50 Rating';
        }
        return '+17 HP';
    case 60: return '---';  // Mystery Key
    case 61:  // Face Steak
        if (instance_exists(obj_ratings_master)) {
            return '+60 HP, +700 Rating';
        }
        return '+60 HP';
    case 62:  // Hush Puppy
        if (instance_exists(obj_endogeny)) {
            return '+65 HP, Mercy';
        }
        return '+65 HP';
    case 63:  // Snail Pie
        if (instance_exists(obj_asgoreb)) {
            return 'Max-1 HP, -1 EnemyDF, -5 EnemyAT';
        }
        if (instance_exists(obj_ripoff_toriel)) {
            return 'Max-1 HP, +3 Mercy';
        }
        return 'Max-1 HP';
    case 64: return '20 DF, +10 AT';  // Temmie Armor
}
return 'Unknown';
");


MakeScript("scr_complete_roomname", @"
for (var i = 0; room_exists(i); i++) {
    var name = room_get_name(i);
    if (string_starts_with(name, argument0)) {
        return name;
    }
    if (string_starts_with(name, 'room_' + argument0)) {
        return string_replace(name, 'room_', '');
    }
}
return '';
");

// Idk how people were able to use GMS1, this sucks ass.
// Recreation of GMS 2.3 function.
MakeScript("string_starts_with", @"
var i = 1;
var len = min(string_length(argument0), string_length(argument1));
while (i <= len) {
    if (string_byte_at(argument0, i) != string_byte_at(argument1, i)) {
        return false;
    }
    i++;
}
return true;
");

GetObject("obj_time").Visible = true;

AppendCode(EventHandler("obj_battlecontroller", EventSubtypeOther.RoomEnd), "global.battlegroup = 0;");

AppendCode(Data.Scripts.ByName("scr_steps").Code, @"
if (alldead == 0) {
    global._stepmin = floor(argument0 * populationfactor);
    global._stepmax = floor((argument0 + argument1) * populationfactor);
}
else if (alldead == 1) {
    global._stepmin = (argument0 + round(argument1 / 2)) * 5;
    global._stepmax = global._stepmin;
}
else if (alldead == 2) {
    global._stepmin = 20;
    global._stepmax = 20;
}
");


// Show first Toriel event trigger in room_ruins3
GetObject("obj_torieltrigger2").Visible = true;
ReplaceCode(EventHandler("obj_torieltrigger2", EventSubtypeDraw.Draw), @"
if (global._hitbox) {
    draw_set_alpha(1);
    draw_set_color(c_aqua);

    // Coords taken from gml_Object_obj_torieltrigger2_Step_0 and adjusted for bbox
    // The numbers in parens are not important, they could be -inf and inf.
    draw_line(160, (188), 160, (240));
    draw_line((0), 188, (160), 188);
}
");

// Show Alphys' "press 3rd switch" call trigger in room_fire10
GetObject("obj_triswitchevent").Visible = true;
ReplaceCode(EventHandler("obj_triswitchevent", EventSubtypeDraw.Draw), @"
if (global._hitbox) {
    draw_set_alpha(1);
    draw_set_color(c_aqua);

    // Coords taken from gml_Object_obj_triswitchevent_Step_0 and adjusted for bbox
    // Unfortunately, mainchara's bbox is wider than the trigger,
    // so the (irrelevant) left side is kinda wrong.
    // (as a rectangle, this would theoretically need negative width)
    draw_line(389, (-1), 389, 108, true);
}
");


// Fixes for noclip sometimes not working
FindReplaceCode(EventHandler("obj_redswitch_1", EventType.Alarm, 3u), "obj_solidparent.solid = 1;", "");
FindReplaceCode(EventHandler("obj_torhandhold1", EventSubtypeStep.EndStep), "obj_spiketile2.solid = 1;", "");
FindReplaceCode(EventHandler("obj_holedown", EventType.Alarm, 2u), "obj_solidparent.solid = 1;", "");
FindReplaceCode(EventHandler("obj_asrielbed", EventSubtypeStep.BeginStep), "obj_solidsmall.solid = 1;", "");
FindReplaceCode(EventHandler("obj_holeup", EventType.Alarm, 2u), "obj_solidparent.solid = 1;", "");
FindReplaceCode(EventHandler("obj_holeup2", EventType.Alarm, 2u), "obj_solidparent.solid = 1;", "");
FindReplaceCode(EventHandler("obj_superdrophole", EventType.Alarm, 3u), "solid = 1;", "");

// Change parent from obj_readable to obj_readablesolid but get rid of `solid = 1`
GetObject("obj_convenientlamp").ParentId = GetObject("obj_readablesolid");
FindReplaceCode(EventHandler("obj_convenientlamp", EventSubtypeStep.BeginStep), "solid = 1;", "");

// Introduce parent obj_solidparent and get rid of `solid = 1`
GetObject("obj_torieltrigger2").ParentId = GetObject("obj_solidparent");
GetObject("obj_torieltrigger2").Solid = false;
AppendCode(EventHandler("obj_torieltrigger2", EventType.Create, 0u), "phase = 0;");

// TODO: gml_Object_obj_shockblock_Step_0, gml_Object_obj_papyrus_guide_Step_1

importGroup.Import();
watermark.Content = WATERMARK;
ScriptMessage($"Successfully patched BioMod v{MOD_VERSION}!\nPress F1 for help.");

////////////////////////////////////////////////// HELPERS
class Err : Exception
{
    const string END = "\n\n(This data file is now probably broken, please reload.)\n\nYou can ignore this garbage:";
    public Err() { }
    public Err(string message) : base("\n" + message + END) { }
    public Err(string message, Exception inner) : base("\n" + message + END, inner) { }
}

void MakeMacro(string name, string value, bool onlyAllowIdentifier = false)
{
    if (!IsValidIdentifier(name))
        throw new Err($"Invalid name {Escape(name)} for macro with value {Escape(value)}");
    if (onlyAllowIdentifier && !IsValidIdentifier(value))
        throw new Err($"Invalid macro value identifier {Escape(value)} for macro {Escape(name)}");
    Macros.Add((name, value));
}

void MakeMacro<T>(string name, T number, T min, T max) where T : System.Numerics.INumber<T>
{
    if (number < min || number > max)
        throw new Err($"Constant {name} is {number} which is not in the allowed range {min} - {max}");
    MakeMacro(name, number.ToString());
}

void MakeKeyMacro(string name, EventSubtypeKey key)
{
    MakeMacro("KEY_" + name, '"' + KeyName(key) + '"');
}

string ApplyMacros(string sourceCode)
{
    // not very performant but who cares
    // TODO: should this resolve macros recursively?
    foreach (var (name, value) in Macros)
    {
        sourceCode = sourceCode.Replace(name, value);
    }
    return sourceCode;
}

void ReplaceCode(UndertaleCode codeEntry, string sourceCode)
{
    importGroup.QueueReplace(codeEntry, ApplyMacros(sourceCode));
}

void PrependCode(UndertaleCode codeEntry, string sourceCode)
{
    importGroup.QueuePrepend(codeEntry, ApplyMacros(sourceCode));
}

void AppendCode(UndertaleCode codeEntry, string sourceCode)
{
    importGroup.QueueAppend(codeEntry, ApplyMacros(sourceCode));
}

void FindReplaceCode(UndertaleCode codeEntry, string query, string sourceCode)
{
    importGroup.QueueFindReplace(codeEntry, query, ApplyMacros(sourceCode));
}

string Escape(string str) => System.Text.Json.JsonSerializer.Serialize(str);

bool IsValidIdentifier(string ident)
{
    if (ident is null || ident.Length < 1) return false;
    if (char.IsAsciiDigit(ident[0])) return false;
    foreach (char c in ident)
    {
        if (c == '_' || c == '@') continue;
        if (char.IsAsciiLetter(c)) continue;
        if (char.IsAsciiDigit(c)) continue;
        return false;
    }
    return true;
}

void MapObjects()
{
    for (int i = 0; i < Data.GameObjects.Count; i++)
    {
        UndertaleGameObject obj = Data.GameObjects[i];
        string name = obj.Name.Content;
        ObjectMap.Add(name, (i, obj));
    }
}

UndertaleGameObject GetObject(string name)
{
    if (ObjectMap.TryGetValue(name, out var pair))
    {
        var (idx, obj) = pair;
        return obj;
    }
    throw new Err($"Could not find game object with name {Escape(name)}, are you sure this is Undertale?");
}

int GetObjectIndex(string name)
{
    if (ObjectMap.TryGetValue(name, out var pair))
    {
        var (idx, obj) = pair;
        return idx;
    }
    throw new Err($"Could not find game object with name {Escape(name)}, are you sure this is Undertale?");
}

UndertaleCode EventHandler(UndertaleGameObject obj, EventType type, uint subtype) => obj.EventHandlerFor(type, subtype, Data);
UndertaleCode EventHandler(string obj, EventType type, uint subtype) => EventHandler(GetObject(obj), type, subtype);

UndertaleCode EventHandler(UndertaleGameObject obj, EventSubtypeDraw drawType) => EventHandler(obj, EventType.Draw, (uint)drawType);
UndertaleCode EventHandler(string obj, EventSubtypeDraw drawType) => EventHandler(GetObject(obj), drawType);

UndertaleCode EventHandler(UndertaleGameObject obj, EventSubtypeOther otherType) => EventHandler(obj, EventType.Other, (uint)otherType);
UndertaleCode EventHandler(string obj, EventSubtypeOther otherType) => EventHandler(GetObject(obj), otherType);

UndertaleCode EventHandler(UndertaleGameObject obj, EventSubtypeStep stepType) => EventHandler(obj, EventType.Step, (uint)stepType);
UndertaleCode EventHandler(string obj, EventSubtypeStep stepType) => EventHandler(GetObject(obj), stepType);

UndertaleCode EventHandler(UndertaleGameObject obj, EventType type, EventSubtypeKey key)
{
    if (type is not (EventType.Keyboard or EventType.KeyPress or EventType.KeyRelease))
        throw new Err($"Invalid event type {type} for keyboard handler");
    return EventHandler(obj, type, (uint)key);
}
UndertaleCode EventHandler(string obj, EventType type, EventSubtypeKey key) => EventHandler(GetObject(obj), type, key);

void RegisterHotkey(EventSubtypeKey key, string code, bool isGlobal = false, bool hold = false)
{
    string objectName = isGlobal ? "obj_time" : "obj_mainchara";
    EventType eventType = hold ? EventType.Keyboard : EventType.KeyPress;
    if (isGlobal)
    {
        code = "if (!instance_exists(obj_essaystuff)) {" + code + "}";
    }
    code = "if (!instance_exists(obj_roomswitcher)) {" + code + "}";
    ReplaceCode(EventHandler(objectName, eventType, key), code);
}

void ShowEvent(string objectName, bool render = false)
{
    ReplaceCode(EventHandler(objectName, EventSubtypeDraw.DrawEnd), @"
if (global._hitbox) {
    draw_set_color(c_aqua);
    draw_rectangle(bbox_left, bbox_top, bbox_right, bbox_bottom, true);
}
    ");
    if (!render) SuppressRendering(objectName);
}

UndertaleCode? FindFriskCollision(UndertaleGameObject obj)
{
    // this is needed to prevent lots of empty events being created
    // (if i used EventHandlerFor instead).
    // TODO: if my PR gets merged, this can be simplified.
    int obj_mainchara_idx = GetObjectIndex("obj_mainchara");
    var events = obj.Events[(int)EventType.Collision];
    foreach (Event ev in events)
    {
        if (ev.EventSubtype == obj_mainchara_idx && ev.Actions.Count > 0)
        {
            return ev.Actions[0].CodeId;
        }
    }
    return null;
}

void MakeScript(string scriptName, string sourceCode)
{
    if (!IsValidIdentifier(scriptName))
    {
        throw new Err($"Invalid script name {Escape(scriptName)}");
    }
    importGroup.QueueReplace("gml_Script_" + scriptName, ApplyMacros(sourceCode));
}

void ImplNoclip()
{
    var obj = GetObject("obj_mainchara");
    var events = obj.Events[(int)EventType.Collision];
    foreach (var ev in events)
    {
        foreach (var action in ev.Actions)
        {
            FindReplaceCode(action.CodeId, "global.phasing == 0", "!(global.phasing || global._noclip)");
        }
    }
}

void ImplRoomSwitcher()
{
    UndertaleGameObject roomswitcher = new()
    {
        Name = Data.Strings.MakeString("obj_roomswitcher"),
        Visible = true,
        Persistent = true,
    };
    Data.GameObjects.Add(roomswitcher);

    ReplaceCode(EventHandler(roomswitcher, EventType.Create, 0u), @"
keyboard_string = '';
text = '';
length = 0;
cursor = 0;
    ");
    ReplaceCode(EventHandler(roomswitcher, EventType.Destroy, 0u), "global.interact = 0;");
    ReplaceCode(EventHandler(roomswitcher, EventType.KeyPress, vk_left), @"
if (keyboard_check(vk_control)) {
    cursor = 0;
} else if (cursor > 0)  {
    cursor--;
}
    ");
    ReplaceCode(EventHandler(roomswitcher, EventType.KeyPress, vk_right), @"
if (keyboard_check(vk_control)) {
    cursor = length;
} else if (cursor < length)  {
    cursor++;
}
    ");
    ReplaceCode(EventHandler(roomswitcher, EventType.KeyPress, vk_enter), @"
instance_destroy();
var room_index = scr_parse_room(text);
if (room_index) {
    room = room_index;
}
    ");
    ReplaceCode(EventHandler(roomswitcher, EventType.KeyPress, vk_tab), @"
var name = scr_complete_roomname(text);
if (name != '') {
    text = name;
    length = string_length(name);
    cursor = length;
}
    ");
    ReplaceCode(EventHandler(roomswitcher, EventType.KeyPress, vk_backspace), @"
if (keyboard_check(vk_control)) {
    text = '';
    length = 0;
    cursor = 0;
} else if (length > 0 && cursor > 0) {
    text = string_delete(text, cursor, 1);
    length--;
    cursor--;
}
    ");
    ReplaceCode(EventHandler(roomswitcher, EventType.KeyPress, vk_delete), @"
if (keyboard_check(vk_control)) {
    text = '';
    length = 0;
    cursor = 0;
} else if (length > 0 && cursor < length) {
    text = string_delete(text, cursor+1, 1);
    length--;
}
// remove shitass DEL character that the runner apparently thinks is printable
keyboard_string = string_delete(keyboard_string, string_length(keyboard_string), 1);
    ");
    ReplaceCode(EventHandler(roomswitcher, EventSubtypeStep.Step), @"
global.interact = 1;
if (keyboard_string != '') {
    var inslen = string_length(keyboard_string);
    text = string_insert(keyboard_string, text, cursor+1);
    length += inslen;
    cursor += inslen;
    keyboard_string = '';
}
    ");
}

bool IsVisible(UndertaleGameObject obj)
{
    if (!obj.Visible) return false;

    // If a regular Draw event exists, the object is probably not actually drawn.
    var events = obj.Events[(int)EventType.Draw];
    foreach (Event ev in events)
    {
        if (ev.EventSubtype == (uint)EventSubtypeDraw.Draw && ev.Actions.Count > 0)
        {
            return false;
        }
    }

    return true;
}

void SuppressRendering(UndertaleGameObject obj)
{
    // This makes sure that there is a regular Draw event.
    // If the runner sees that there is such an event,
    // it will not draw the object using normal rules
    // and instead relies on this event.
    // Since this event will (probably) be empty, this
    // effectively makes this object invisible.
    // This is different from simply setting `visible = false`,
    // because this way, other draw events such as DrawEnd are still called.
    EventHandler(obj, EventSubtypeDraw.Draw);
}
void SuppressRendering(string objectName) => SuppressRendering(GetObject(objectName));


bool IsParent(UndertaleGameObject obj, UndertaleGameObject parentMaybe)
{
    while (obj?.ParentId is UndertaleGameObject parent)
    {
        if (parent == parentMaybe) return true;
        obj = parent;
    }
    return false;
}
bool IsParent(string obj, UndertaleGameObject parentMaybe) => IsParent(GetObject(obj), parentMaybe);
bool IsParent(UndertaleGameObject obj, string parentMaybe) => IsParent(obj, GetObject(parentMaybe));
bool IsParent(string obj, string parentMaybe) => IsParent(GetObject(obj), GetObject(parentMaybe));

bool CheckAlreadyPatched()
{
    const string LOADVANILLA = "Please load a vanilla data file and try again.";
    foreach (UndertaleString utstring in Data.Strings)
    {
        string str = utstring?.Content ?? "";
        if (str.StartsWith(MAGIC))
        {
            if (str == WATERMARK)
            {
                ScriptMessage("Already patched!");
            }
            else if (str.EndsWith("_BROKEN"))
            {
                ScriptError("This data file is broken because BioMod previously crashed while patching. " + LOADVANILLA);
            }
            else
            {
                string v = str.Substring(MAGIC.Length);
                ScriptError($"Found existing BioMod v{v}. This script patches BioMod v{MOD_VERSION}. " + LOADVANILLA);
            }
            return true;
        }
    }
    return false;
}

string BattlegroupCode => @"
switch (argument0) {
    case 1: return 'Test Monster';
    case 2: return 'Dummy';
    case 3: return 'Fake Froggit';
    case 4: return 'Froggit';
    case 5: return 'Whimsun';
    case 6: return 'Whimsun & Froggit';
    case 7: return 'Moldsmal';
    case 8: return 'Moldsmal & Moldsmal & Moldsmal';
    case 9: return 'Froggit & Froggit';
    case 10: return 'Moldsmal & Moldsmal (Ruins)';
    case 11: return 'Moldsmal & Migosp';
    case 12: return 'Migosp & Vegetoid';
    case 13: return 'Loox';
    case 14: return 'Loox & Vegetoid';
    case 15: return 'Loox & Vegetoid & Migosp';
    case 16: return 'Vegetoid & Vegetoid';
    case 17: return 'Loox & Loox';
    case 18: return 'Vegetoid';
    case 19: return 'Too much';
    case 20: return 'Napstablook';
    case 21: return 'Loox';
    case 22: return 'Toriel';
    case 23: return 'Doggo';
    case 24: return 'Lesser Dog';
    case 25: return 'Dogamy & Dogaressa';
    case 26: return 'Greater Dog';
    case 27: return 'Papyrus';
    case 28: return 'Gyftrot';
    case 30: return 'Snowdrake';
    case 31: return 'Chilldrake & Chilldrake';
    case 32: return 'Icecap';
    case 33: return 'Icecap & Snowdrake';
    case 34: return 'Jerry';
    case 35: return 'Icecap & Jerry';
    case 36: return 'Icecap & Jerry & Snowdrake';
    case 40: return 'Aaron';
    case 41: return 'Temmie';
    case 42: return 'Moldsmal & Moldbygg';
    case 43: return 'Woshua';
    case 44: return 'Shyren';
    case 45: return 'Mad Dummy';
    case 46: return 'Aaron & Woshua';
    case 47: return 'Undyne';
    case 48: return 'Mettaton - Quiz';
    case 49: return 'Royal Guard 01 & Royal Guard 02';
    case 50: return 'Tsunderplane';
    case 51: return 'Vulkin';
    case 52: return 'Pyrope';
    case 53: return 'Moldsmal & Moldsmal';
    case 54: return 'Woshua & Aaron';
    case 55: return 'Moldbygg & Woshua';
    case 56: return 'Muffet';
    case 57: return 'Mettaton - Tile Puzzle';
    case 58: return 'Undyne - Date';
    case 59: return 'Madjick';
    case 60: return 'Knight Knight';
    case 61: return 'Final Froggit';
    case 62: return 'Astigmatism';
    case 63: return 'Whimsalot';
    case 64: return 'Whimsalot & Final Froggit';
    case 65: return 'Whimsalot & Astigmatism';
    case 66: return 'Final Froggit & Astigmatism';
    case 67: return 'Final Froggit & Astigmatism & Whimsalot';
    case 68: return 'Knight Knight & Madjick';
    case 69: return 'Bomb';
    case 70: return 'Dog Bomb';
    case 71: return 'Extremely agile glass of water';
    case 72: return 'Script Bomb';
    case 73: return 'Basketball Bomb';
    case 74: return 'Present Bomb';
    case 75: return 'Game Bomb';
    case 76: return 'Royal Guard 03 & Royal Guard 04';
    case 77: return 'Tsunderplane & Vulkin';
    case 78: return 'Pyrope & Pyrope';
    case 79: return 'Vulkin & Vulkin';
    case 80: return 'Mettaton - CORE';
    case 81: return 'Mettaton EX';
    case 82: return 'Lemon Bread';
    case 83: return 'Reaper Bird';
    case 84: return 'Snowdrake Mom';
    case 85: return 'Memory Head & Memory Head & Memory Head';
    case 86: return 'Endogeny';
    case 87: return 'Ripoff Undyne';
    case 88: return 'Ripoff Papyrus & Ripoff Sans';
    case 89: return 'Ripoff Alphys';
    case 90: return 'Ripoff Toriel & Ripoff Asgore';
    case 91: return 'Monster Kid';
    case 92: return 'Undyne the Undying';
    case 93: return 'Glad Dummy';
    case 94: return 'Mettaton NEO';
    case 95: return 'Sans';
    case 100: return 'Asgore Intro';
    case 101: return 'Asgore';
    case 120: return 'Final Froggit & Astigmatism [HARDMODE]';
    case 121: return 'Final Froggit & Migospel [HARDMODE]';
    case 122: return 'Parsnik [HARDMODE]';
    case 123: return 'Moldessa & Moldessa [HARDMODE]';
    case 124: return 'Moldessa & Moldessa & Moldessa [HARDMODE]';
    case 125: return 'Final Froggit & Whimsalot [HARDMODE]';
    case 126: return 'Final Froggit [HARDMODE]';
    case 127: return 'Whimsalot & Parsnik [HARDMODE]';
    case 128: return 'Moldessa & Migospel [HARDMODE]';
    case 129: return 'Parsnik & Migospel [HARDMODE]';
    case 130: return 'Parsnik & Parsnik [HARDMODE]';
    case 131: return 'Parsnik & Astigmatism [HARDMODE]';
    case 132: return 'Astigmatism & Astigmatism [HARDMODE]';
    case 133: return 'Astigmatism & Migospel & Moldessa [HARDMODE]';
    case 134: return 'Whimsalot & Parsnik & Moldessa [HARDMODE]';
    case 135: return 'Glyde';
    case 140: return 'So Sorry';
    case 255: return 'Asriel - God of Hyperdeath';
    case 256: return 'Asriel - Final Form';
}
return 'Unknown';
";

string KeyName(EventSubtypeKey key) => key switch
{
    vk_nokey => "No",
    vk_anykey => "Any",
    vk_backspace => "Bksp",
    vk_tab => "Tab",
    vk_return | vk_enter => "Ent",
    vk_shift => "Shf",
    vk_control => "Ctrl",
    vk_alt => "Alt",
    vk_pause => "Ps",
    vk_escape => "Esc",
    vk_space => "Sp",
    vk_pageup => "PgUp",
    vk_pagedown => "PgDn",
    vk_end => "End",
    vk_home => "Hm",
    vk_left => "<",
    vk_up => "Up",
    vk_right => ">",
    vk_down => "Down",
    vk_printscreen => "Prt",
    vk_insert => "Ins",
    vk_delete => "Del",
    Digit0 => "0",
    Digit1 => "1",
    Digit2 => "2",
    Digit3 => "3",
    Digit4 => "4",
    Digit5 => "5",
    Digit6 => "6",
    Digit7 => "7",
    Digit8 => "8",
    Digit9 => "9",
    vk_numpad0 => "Num0",
    vk_numpad1 => "Num1",
    vk_numpad2 => "Num2",
    vk_numpad3 => "Num3",
    vk_numpad4 => "Num4",
    vk_numpad5 => "Num5",
    vk_numpad6 => "Num6",
    vk_numpad7 => "Num7",
    vk_numpad8 => "Num8",
    vk_numpad9 => "Num9",
    vk_multiply => "Mul",
    vk_add => "Add",
    vk_subtract => "Sub",
    vk_decimal => "Dec",
    vk_divide => "Div",
    vk_f1 => "F1",
    vk_f2 => "F2",
    vk_f3 => "F3",
    vk_f4 => "F4",
    vk_f5 => "F5",
    vk_f6 => "F6",
    vk_f7 => "F7",
    vk_f8 => "F8",
    vk_f9 => "F9",
    vk_f10 => "F10",
    vk_f11 => "F11",
    vk_f12 => "F12",
    vk_lshift => "LShift",
    vk_rshift => "RShift",
    vk_lcontrol => "LCtrl",
    vk_rcontrol => "RCtrl",
    vk_lalt => "LAlt",
    vk_ralt => "RAlt",
    _ => key.ToString(),
};
