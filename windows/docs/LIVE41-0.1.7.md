# Rovunet Windows 0.1.7 Beta

## فارسی

**نمای ویندوز از نو ساخته شد، این بار روی همان زبان طراحی اندروید.** رنگ‌ها از
همان نقشهٔ Material اندروید گرفته شده‌اند: پس‌زمینهٔ `#F5F5F7` در روشن، `#15171D`
در تیره و `#000000` کامل در AMOLED، سطح‌ها روی `#EDF1F6` / `#252F3E` / `#171D27`،
متن و خط‌ها هم از همان پالت. دکمه‌ها، تب‌های ناوبری و ردیف‌های منو به شکل کپسولی
درآمدند، سطح‌ها تخت شدند و شعاع گوشه‌ها بزرگ‌تر شد تا با اندروید یکی به نظر برسند.

**رنگ‌های لهجه در AMOLED هم کار می‌کنند.** تیرهٔ AMOLED در پوستهٔ HTML متغیرهای
رنگ را دوباره تعریف می‌کرد و مقدار انتخاب‌شده را زیر می‌گرفت؛ به همین دلیل در
AMOLED هیچ رنگی جز پیش‌فرض اثر نداشت. حالا رنگ لهجه در همان جایی نوشته می‌شود که
تم نوشته می‌شود و سه مشتق آن (`group`، `soft`، `ring`) هم از همان رنگ ساخته
می‌شوند، پس انتخاب رنگ در روشن، تیره و AMOLED یکسان دیده می‌شود.

**هشت لهجهٔ اندروید.** فهرست رنگ‌ها دقیقاً همان مقادیر اندروید است (`default`,
`teal`, `blue`, `green`, `amber`, `orange`, `pink`, `purple`) و در تنظیمات با نام
ترجمه‌شده و نمونهٔ رنگ نمایش داده می‌شود؛ در حالت کنتراست بالا هنوز رنگ سیستم حاکم است.

**بستهٔ عمومی هنوز Free را ندارد.** کلاینت Rovunet Free v2 فقط در نسخهٔ Debug
فعال است و بستهٔ 0.1.7 بدون کلید آزمایشی ساخته شده است؛ اسکریپت بسته‌بندی اگر
`free-v2.local.props` در درخت باشد از ساخت بستهٔ عمومی امتناع می‌کند.

**محدودیت‌ها:** نصب‌کننده هنوز امضای Authenticode ندارد، پس SmartScreen ممکن است
هشدار بدهد. TUN و kill switch مثل قبل در این نسخه فعال نیستند.

## English

**The Windows view was rebuilt on Android's design language.** Colours now come
from the same Material roles Android uses: `#F5F5F7` background in light,
`#15171D` in dark and true `#000000` in AMOLED, with surfaces on `#EDF1F6`,
`#252F3E` and `#171D27`, and the same ink and outline values. Buttons, navigation
tabs and menu rows became pills, surfaces were flattened and corner radii grew so
the two apps read as one product.

**Accent colours now work in AMOLED too.** The AMOLED block re-declared the
colour variables inside the HTML shell and shadowed the chosen accent, which is
why only light and dark appeared to change anything. The accent is now written
where the theme itself is written, and its three derived tones (`group`, `soft`,
`ring`) are generated from the same colour, so an accent looks the same in light,
dark and AMOLED.

**Android's eight accents.** The palette is the exact Android enum (`default`,
`teal`, `blue`, `green`, `amber`, `orange`, `pink`, `purple`), shown in Settings
with a translated name and a colour dot; high contrast still wins.

**The public package still has no Free source.** The Rovunet Free v2 client stays
debug-only, and 0.1.7 is built without the debug key; the packaging script
refuses to produce a public package while `free-v2.local.props` is in the tree.

**Limits:** the installer is not Authenticode-signed, so SmartScreen may warn.
TUN and the kill switch remain inactive in this consumer beta.
