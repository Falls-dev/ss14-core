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

        msg = Regex.Replace(msg, @"(?<!\w)\bженщина\b",  _random.Pick(new List<string>() { "манда", "мадмуазель", "милая", "красотка", "шлюха" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдевушка\b",  _random.Pick(new List<string>() { "манда", "мадмуазель", "милая", "красотка", "шлюха" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдевочка\b",  _random.Pick(new List<string>() { "манда", "мадмуазель", "милая", "красотка", "шлюха" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдоча\b",  _random.Pick(new List<string>() { "манда", "мадмуазель", "милая", "красотка", "шлюха" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bмужик\b",  _random.Pick(new List<string>() { "пацан", "мудила", "гандонио", "мудаёб", "подкаблучник" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bпарень\b",  _random.Pick(new List<string>() { "пацан", "мудила", "гандонио", "мудаёб", "подкаблучник" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bчувак\b",  _random.Pick(new List<string>() { "пацан", "мудила", "гандонио", "мудаёб", "подкаблучник" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bчел\b",  _random.Pick(new List<string>() { "пацан", "мудила", "гандонио", "мудаёб", "подкаблучник" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bубит\b",  _random.Pick(new List<string>() { "кокнут", "насажен", "загашен", "угандошен", "ёбнут" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bубил\b",  _random.Pick(new List<string>() { "кокнул", "насадил", "загасил", "угандошил", "ёбнул" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bубили\b",  _random.Pick(new List<string>() { "кокнули", "насадили", "загасили", "угандошили", "ёбнули" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bассистент\b",  _random.Pick(new List<string>() { "браток", "братан", "новичок", "акробат" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bассистента\b",  _random.Pick(new List<string>() { "братка", "братана", "новичка", "акробата" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bасик\b",  _random.Pick(new List<string>() { "браток", "братан", "новичок", "акробат" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bасистуха\b",  _random.Pick(new List<string>() { "браток", "братан", "новичок", "акробат" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bглава\b",  _random.Pick(new List<string>() { "начальник", "контролёр", "заправляющий" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bсобака\b",  _random.Pick(new List<string>() { "сука", "сучка" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bсигарета\b",  _random.Pick(new List<string>() { "косяк", "косячок", "бычок" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bсигарету\b",  _random.Pick(new List<string>() { "косяка", "косячка", "бычка" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bсигаретку\b",  _random.Pick(new List<string>() { "косяка", "косячка", "бычка" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bалкоголик\b",  _random.Pick(new List<string>() { "шатун", "алканавт", "запойный", "бражник" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bалкоголика\b",  _random.Pick(new List<string>() { "шатуна", "алканават", "запойного", "бражника" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bалкаш\b",  _random.Pick(new List<string>() { "шатун", "алканавт", "запойный", "бражник" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bалкаша\b",  _random.Pick(new List<string>() { "шатуна", "алканават", "запойного", "бражника" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bпиво\b",  _random.Pick(new List<string>() { "бухло", "моча", "зелье" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bпива\b",  _random.Pick(new List<string>() { "бухла", "мочи", "зельеца" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bпивка\b",  _random.Pick(new List<string>() { "бухла", "мочи", "зельеца" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bалкашки\b",  _random.Pick(new List<string>() { "бухла", "мочи", "зельеца" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bводка\b",  _random.Pick(new List<string>() { "баян", "газ", "сапог", "хань" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bводки\b",  _random.Pick(new List<string>() { "баяна", "газа", "сапога", "хани" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bводяры\b",  _random.Pick(new List<string>() { "баяна", "газа", "сапога", "хани" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bводочки\b",  _random.Pick(new List<string>() { "баяна", "газа", "сапога", "хани" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bтерпила\b",  _random.Pick(new List<string>() { "немощь", "лошара", "опущенец" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bжопа\b",  _random.Pick(new List<string>() { "очко", "дристалище", "гузно" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bжопе\b",  _random.Pick(new List<string>() { "очке", "дристалище" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bфелинид\b",  _random.Pick(new List<string>() { "хвостатый", "ушастый", "мерзкий", "лизун" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bфелинида\b",  _random.Pick(new List<string>() { "хвостатого", "ушастого", "мерзкого", "лизуна" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bфелинидка\b",  _random.Pick(new List<string>() { "хвостатая", "ушастая", "мерзкая", "лизунщица" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкотик\b",  _random.Pick(new List<string>() { "хвостатый", "ушастый", "мерзкий", "лизун" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкотика\b",  _random.Pick(new List<string>() { "хвостатая", "ушастая", "мерзкая", "лизунщица" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bгарпия\b",  _random.Pick(new List<string>() { "горливая", "птенчик", "крылатый", "яйцеукладчик" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bгарпию\b",  _random.Pick(new List<string>() { "горливую", "птенчика", "крылатую", "яйцеукладчика" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bящер\b",  _random.Pick(new List<string>() { "тупой", "заумный", "лысый", "гладкий", "яйцеглот" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bящера\b",  _random.Pick(new List<string>() { "тупого", "заумного", "лысого", "гладкого", "яйцеглотателя" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bдворф\b",  _random.Pick(new List<string>() { "карлик", "выёбистый", "работяга", "наглый" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдворфа\b",  _random.Pick(new List<string>() { "карлика", "выёбистого", "работягу", "наглого" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bстанбатон\b",  _random.Pick(new List<string>() { "дрючка", "жезл", "кол" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдубинка\b",  _random.Pick(new List<string>() { "дрючка", "жезл", "кол" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдубина\b",  _random.Pick(new List<string>() { "дрючка", "жезл", "кол" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдубинку\b",  _random.Pick(new List<string>() { "дрючку", "жезл", "кол" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bдизейблер\b",  _random.Pick(new List<string>() { "пукалка", "решала" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bграната\b",  _random.Pick(new List<string>() { "грена", "картошка", "лимонка", "снаряд" }), RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bгранату\b",  _random.Pick(new List<string>() { "грену", "картошку", "лимонку", "снаряд" }), RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bобыск\b", "шмон", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bобыска\b", "шмона", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bобыскивать\b", "шмонать", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bкарта\b", "шепёрка", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкарта\b", "шепёрку", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bбумага\b", "шмага", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bбумагу\b", "шмагу", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bбумбокс\b", "свистоперделка", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bболезнь\b", "воля божья", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bотец\b", "батя", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bотца\b", "батька", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bсвященник\b", "спаситель", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bсвященника\b", "спасителя", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bсвятоша\b", "спаситель", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bсвятошу\b", "спасителя", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bкапитан\b", "затворник", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкапитана\b", "затворника", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкапитаншу\b", "затворницу", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкэпа\b", "затворника", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкэп\b", "затворник", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bинженер\b", "монтёр", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bинженера\b", "монтёра", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bгрузчик\b", "шнырь", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bгрузчика\b", "шныря", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bхоп\b", "шестёрка", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bхопа\b", "шестёрку", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bхос\b", "петух", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bхоса\b", "петуха", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bборг\b", "жестянка", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bборга\b", "жестянку", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bборгов\b", "жестянок", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bприслуга\b", "куртизанка", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bприслугу\b", "куртизанку", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bприслуг\b", "куртизанок", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bученый\b", "яйцеголовый", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bучёный\b", "яйцеголовый", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bучоный\b", "яйцеголовый", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bученые\b", "яйцеголовые", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bутилизатор\b", "каторжник", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bутилизаторы\b", "каторжники", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bшахтёр\b", "каторжник", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bшахтёры\b", "каторжник", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bшахтер\b", "каторжник", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bшахтеры\b", "каторжник", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bофицер\b", "цыплёнок", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bофицеры\b", "цыплята", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bповар\b", "ложкарь", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bуборщик\b", "бык", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bуборщика\b", "быка", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bкм\b", "глава шнырей", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкма\b", "главу шнырей", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bквартимейстер\b", "глава шнырей", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bквартимейстера\b", "главу шнырей", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bбиблиотекарь\b", "зануда", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bбиблиотекаря\b", "зануду", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bбиблиотекаря\b", "зануде", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bклоун\b", "проказник", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bклоуна\b", "проказника", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bклоуну\b", "проказнику", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bмим\b", "молчун", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bмима\b", "молчуна", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bмиму\b", "молчуну", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bдетектив\b", "следак", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдетектива\b", "следака", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bдетективу\b", "следаку", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bюрист\b", "немощь", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bюриста\b", "немоща", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bюристу\b", "немощу", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bагент\b", "ляпаш", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bагента\b", "ляпашку", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bагенту\b", "ляпашнику", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bагентам\b", "ляпашникам", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bнт\b", "контора пидорасов", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bнанотрейзен\b", "контора пидорасов", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bцк\b", "хозяева", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bшаттл\b", "аппарат", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bшаттлу\b", "аппарату", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bшаттла\b", "аппарата", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bшатл\b", "аппарат", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bшатлу\b", "аппарату", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bшатла\b", "аппарата", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bкадет\b", "яйцо", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкадета\b", "яйцо", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bварден\b", "зрячий", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bвардена\b", "зрячего", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bинспектор\b", "бесполезный", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bинспектора\b", "бесполезного", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bеда\b", "похлёбка", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bеды\b", "похлёбки", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bглаза\b", "моргала", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bуши\b", "локаторы", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bэвакуация\b", "последний свет", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bэвакуацию\b", "последний свет", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bэвак\b", "последний свет", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bэвака\b", "последнего света", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bкамера\b", "аквариум", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bкамеру\b", "аквариум", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bперма\b", "отрезвитель", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bперму\b", "отрезвитель", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bопера\b", "оркестр", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bопер\b", "актёр", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bоперу\b", "актёру", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bмусорка\b", "столовая", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bмусорку\b", "столовую", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bемаг\b", "хуйня", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bноуслипы\b", "блестяшки", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bпда\b", "копьютер", RegexOptions.IgnoreCase);

        return msg;
    }

    private void OnAccent(EntityUid uid, BomzhAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
