using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class BomzhAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BomzhAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message, BomzhAccentComponent component)
    {
        var msg = message;
        msg = _replacement.ApplyReplacements(msg, "bomzh");

        msg = Regex.Replace(msg, "а+", _random.Pick(new List<string>() { "а..", "аа.." }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, "э+", _random.Pick(new List<string>() { "э..", "ээ.." }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, "я+", _random.Pick(new List<string>() { "ам..", "я.." }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, "у+", _random.Pick(new List<string>() { "уэ..", "уи..", "уа.." }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, "у+", _random.Pick(new List<string>() { "уэ..", "уи..", "уа.." }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bда",  _random.Pick(new List<string>() { "пизда", "я...", "где..", "кто.." }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bженщина",  _random.Pick(new List<string>() { "манда", "мадмуазель", "милая", "красотка" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдевушка",  _random.Pick(new List<string>() { "манда", "мадмуазель", "милая", "красотка" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдевочка",  _random.Pick(new List<string>() { "манда", "мадмуазель", "милая", "красотка" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдоча",  _random.Pick(new List<string>() { "манда", "мадмуазель", "милая", "красотка" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bмужик",  _random.Pick(new List<string>() { "пацан", "мудила", "гандонио", "мудаёб", "подкаблучник" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bпарень",  _random.Pick(new List<string>() { "пацан", "мудила", "гандонио", "мудаёб", "подкаблучник" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bубит",  _random.Pick(new List<string>() { "кокнут", "насажен", "загашен", "угандошен", "ёбнут" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bубил",  _random.Pick(new List<string>() { "кокнул", "насадил", "загасил", "угандошил", "ёбнул" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bубили",  _random.Pick(new List<string>() { "кокнули", "насадили", "загасили", "угандошили", "ёбнули" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bглава",  _random.Pick(new List<string>() { "начальник", "контролёр", "заправляющий" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bсобака",  _random.Pick(new List<string>() { "сука", "сучка" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bсигарета",  _random.Pick(new List<string>() { "косяк", "косячок", "бычок" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bсигарету",  _random.Pick(new List<string>() { "косяка", "косячка", "бычка" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bсигаретку",  _random.Pick(new List<string>() { "косяка", "косячка", "бычка" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bалкоголик",  _random.Pick(new List<string>() { "шатун", "алканавт", "запойный", "бражник" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bалкоголика",  _random.Pick(new List<string>() { "шатуна", "алканават", "запойного", "бражника" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bалкаш",  _random.Pick(new List<string>() { "шатун", "алканавт", "запойный", "бражник" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bалкаша",  _random.Pick(new List<string>() { "шатуна", "алканават", "запойного", "бражника" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bпиво",  _random.Pick(new List<string>() { "бухло", "моча", "зелье" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bпива",  _random.Pick(new List<string>() { "бухла", "мочи", "зельеца" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bводка",  _random.Pick(new List<string>() { "баян", "газ", "сапог", "хань" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bводки",  _random.Pick(new List<string>() { "баяна", "газа", "сапога", "хани" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bводяры",  _random.Pick(new List<string>() { "баяна", "газа", "сапога", "хани" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bводочки",  _random.Pick(new List<string>() { "баяна", "газа", "сапога", "хани" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bтерпила",  _random.Pick(new List<string>() { "немощь", "лошара", "опущенец" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bжопа",  _random.Pick(new List<string>() { "очко", "дристалище", "гузно" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bжопа",  _random.Pick(new List<string>() { "очке", "дристалище" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bфелинид",  _random.Pick(new List<string>() { "хвостатый", "ушастый", "мерзкий", "лизун" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bфелинида",  _random.Pick(new List<string>() { "хвостатого", "ушастого", "мерзкого", "лизуна" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bфелинидка",  _random.Pick(new List<string>() { "хвостатая", "ушастая", "мерзкая", "лизунщица" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bгарпия",  _random.Pick(new List<string>() { "горливая", "птенчик", "крылатый", "яйцеукладчик" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bгарпию",  _random.Pick(new List<string>() { "горливую", "птенчика", "крылатую", "яйцеукладчика" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bящер",  _random.Pick(new List<string>() { "тупой", "заумный", "лысый", "гладкий", "яйцеглот" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bящера",  _random.Pick(new List<string>() { "тупого", "заумного", "лысого", "гладкого", "яйцеглотателя" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bдворф",  _random.Pick(new List<string>() { "карлик", "выёбистый", "работяга", "наглый" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдворфа",  _random.Pick(new List<string>() { "карлика", "выёбистого", "работягу", "наглого" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bстанбатон",  _random.Pick(new List<string>() { "дрючка", "жезл", "кол" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдубинка",  _random.Pick(new List<string>() { "дрючка", "жезл", "кол" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдубина",  _random.Pick(new List<string>() { "дрючка", "жезл", "кол" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдубинку",  _random.Pick(new List<string>() { "дрючку", "жезл", "кол" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bдизейблер",  _random.Pick(new List<string>() { "пукалка", "решала" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bграната",  _random.Pick(new List<string>() { "грена", "картошка", "лимонка", "снаряд" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bграната",  _random.Pick(new List<string>() { "грену", "картошку", "лимонку", "снаряд" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bболезнь", "воля божья", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bотец", "батя", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bотца", "батька", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bсвященник", "спаситель", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bсвященника", "спасителя", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bсвятошу", "спасителя", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bкапитан", "затворник", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкапитана", "затворника", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкапитаншу", "затворницу", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкэпа", "затворника", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкэп", "затворник", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bинженер", "монтёр", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bинженера", "монтёра", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bуборщик", "шнырь", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bуборщика", "шныря", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bхоп", "шестёрка", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bхос", "петух", RegexOptions.IgnoreCase);

        return msg;
    }

    private void OnAccent(EntityUid uid, BomzhAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
