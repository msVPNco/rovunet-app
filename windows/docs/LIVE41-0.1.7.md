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

**باز و بسته کردن منو دیگر هنگ نمی‌کند.** پیش‌تر هر بار باز یا بسته شدن منوی
کناری کل نما از نو ساخته می‌شد؛ حالا منو لایهٔ مستقل خودش است و فقط همان
به‌روز می‌شود. حجم و زمان باقی‌مانده روی کارت بسته هم دیده می‌شود و متن‌های
بلندتر از کادر وقتی حرکت متن در تنظیمات روشن باشد می‌روند.

**تنظیمات دیگر با یک کلیک بسته نمی‌شود.** انتخاب پوسته یا رنگ لهجه صفحهٔ
تنظیمات را می‌بست و کاربر را به خانه برمی‌گرداند. حالا همان صفحه باز می‌ماند،
انتخاب تازه را نشان می‌دهد و فقط با خواست خود کاربر (دکمهٔ بازگشت یا Esc) بسته
می‌شود.

**بزرگ و کوچک کردن نما دیگر مکث یک‌دو ثانیه‌ای ندارد.** تا پیش از این هر تغییر
عرض، نمای پنهانِ WPF و همهٔ کارت‌ها را از نو می‌ساخت و همان کار رشتهٔ رابط را
متوقف می‌کرد. حالا وقتی پوستهٔ HTML در حال نمایش است، تغییر عرض فقط چیدمان و
سربرگ سبک را به‌روز می‌کند و کارت‌ها، فوکوس و موقعیت اسکرول دست‌نخورده می‌مانند.
اندازه‌گیری با ۲۰۰ و ۵۰۰ پروفایل ساختگی، روی همان پنجرهٔ واقعی: پیش از اصلاح هر
تغییر عرض ۸۱۰ تا ۱۱۳۸ میلی‌ثانیه طول می‌کشید، پس از اصلاح ۳ تا ۷ میلی‌ثانیه،
بدون بازسازی کارت‌ها.

**کارت انتخاب‌شده در حالت قطع هم مشخص است.** نشان انتخاب فقط روی ردیفِ داخل
مجموعهٔ باز کشیده می‌شد؛ اگر مجموعه بسته بود یا مجموعهٔ دیگری باز می‌شد، هیچ
نشانی دیده نمی‌شد. حالا سربرگ مجموعهٔ انتخاب‌شده هم حلقهٔ لهجه و پس‌زمینهٔ
متمایز دارد و این نشان به اتصال، فوکوس پنجره یا باز بودن مجموعه وابسته نیست؛
حالت سبز اتصال همچنان جدا می‌ماند.

**متن بلند از کادر بیرون می‌رود، نه اینکه به راست برگردد.** جهت حرکت با جهت
خواندن هماهنگ شد: متن لاتین به چپ و متن فارسی به راست از کادر خارج می‌شود و
حرکت پیوسته ادامه دارد، بدون جهش برگشتی. اندازه‌گیری متن هم پس از آرام شدن
تغییر اندازه یک‌بار انجام می‌شود، نه در هر فریم.

**Rovunet Free با یک دکمه.** در صفحهٔ منابع عمومی، کارت «اتصال رایگان روونت»
دکمهٔ «دریافت اتصال رایگان» دارد و همان مسیر آزمودهٔ پیش‌نمایش و افزودن را باز
می‌کند که امضای Ed25519 کاتالوگ و پاکت AES-256-GCM را پیش از هر اتصال بررسی
می‌کند؛ دقیقاً همان قرارداد `free/v2` که در اندروید پیاده شده است. اگر بیلد
مقادیر کلید و آدرس را نداشته باشد، همان کارت به‌جای دکمه توضیح می‌دهد که این
قابلیت در این نسخه در دسترس نیست و هیچ اکشنی نشان نمی‌دهد.

برای ساخت نسخه‌ای که واقعاً کانفیگ رایگان بدهد، سه مقدار کلید عمومی، کلید AES و
آدرس کاتالوگ (همان‌هایی که در `local.properties` اندروید دارید) در فایل محلی
`windows/free-v2.local.props` قرار می‌گیرد. آن فایل در `.gitignore` است، در
آرشیو منبع نمی‌رود و اسکریپت بسته‌بندی تا وقتی موجود است از ساخت بستهٔ عمومی
امتناع می‌کند؛ یعنی هیچ بستهٔ عمومی‌ای ناخواسته کلید را حمل نمی‌کند.

**انطباق کامل با قرارداد free/v2.** سه بند قرارداد که پیاده‌سازی ویندوز نداشت
اضافه شد: شناسهٔ آیتم داخل متن رمزشده باید با شناسهٔ امضاشده در کاتالوگ یکی
باشد؛ اگر بیلد یک `key_id` ثابت داشته باشد، پاکت‌هایی که شناسهٔ کلید دیگری را
نام می‌برند به‌جای آزمودن با کلید اشتباه کنار گذاشته می‌شوند؛ و انتخاب تصادفی
اول بین گروه‌های اهدا و بعد داخل هر گروه انجام می‌شود تا شش مسیر یک اهداکننده
شبیه شش اهداکنندهٔ مستقل دیده نشود. مجموعهٔ `Rovunet.FreeCatalogTests` حالا
۱۷ آزمون دارد، از جمله AAD با شناسهٔ کلید متفاوت، نسخه یا الگوریتم پشتیبانی‌نشده
و متن رمزشده‌ای که شناسهٔ دیگری را ادعا می‌کند؛ هیچ‌کدام از این‌ها و همچنین
امضای نامعتبر، هش ناهمخوان، کلید نامعتبر و سهمیهٔ نامعتبر پذیرفته نمی‌شود.
آزمون زندهٔ تونل با `Rovunet.FreeLiveTests` و فایل‌های کلید محلی اجرا می‌شود
(`<aes-key-file> <public-key-file> <catalog-base-url> [key-id]`) و تعداد دریافت،
رمزگشایی، اتصال و عبور واقعی داده را جدا گزارش می‌کند؛ این اجرا هنوز انجام نشده
و اتصال واقعی Free تأییدشده نیست.

**زنجیره با هر پروفایل ذخیره‌شده کار می‌کند.** یک شرط اضافی که در اندروید
وجود ندارد، پروفایل‌هایی را که اشتراک‌گذاری دستگاه‌شان خاموش است از لایه‌های
زنجیره حذف می‌کرد؛ نتیجه این بود که فهرست لایهٔ دوم خالی می‌ماند. آن شرط
برداشته شد و اگر برای یک جایگاه واقعاً پروفایل مجازی نباشد، خود ویرایشگر
توضیح می‌دهد.

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

**Rovunet Free in one click.** The public sources page now shows a Rovunet Free
card with a "Get free connection" button that opens the same reviewed preview and
import flow, which verifies the catalog's Ed25519 signature and every AES-256-GCM
envelope before any profile is used — the same `free/v2` contract Android
implements. When the build carries no catalog values, the card explains that the
feature is unavailable in this build instead of offering an action.

To build a Windows release that really hands out free configs, put the three
values you already keep in Android's `local.properties` (public key, AES key,
catalog address) into the local `windows/free-v2.local.props` file. That file is
gitignored, never enters the source archive, and while it exists the packaging
script refuses to produce a public package — so no public build ships the key by
accident.

**Full `free/v2` contract conformance.** Three contract clauses the Windows
client was missing are now in place: the plaintext must carry the same item id
the catalog signed; when a build pins a `key_id`, envelopes naming another key
are skipped instead of being tried against the wrong secret; and selection is
random by donation group first, then within the group, so six paths of one
donor never look like six independent donors. `Rovunet.FreeCatalogTests` now
runs 17 checks, adding a mismatched AAD key id, an unsupported envelope
version or algorithm, and a plaintext claiming another id — alongside the
existing rejection of a bad signature, a hash mismatch, a wrong key, an
invalid quota and an unsorted catalog. The real tunnel probe runs with local
key files through `Rovunet.FreeLiveTests`
(`<aes-key-file> <public-key-file> <catalog-base-url> [key-id]`) and reports
fetched, decrypted, connected and payload-verified counts separately. That
run has not happened, so real Free connectivity is not verified.

**The side menu no longer stutters.** Opening or closing it used to rebuild the
whole view; the menu is now its own layer and only that layer is redrawn. Usage
and remaining time show on a collapsed collection card too, and text wider than
its box scrolls when text motion is enabled in Settings.

**Chains accept every saved profile.** An extra condition that Android does not
have dropped profiles whose device sharing is off from chain layers, which left
the second layer list empty; it is gone, and when no profile really fits a
position the editor now says so.

**Limits:** the installer is not Authenticode-signed, so SmartScreen may warn.
TUN and the kill switch remain inactive in this consumer beta.
