# Rovunet Windows 0.1.6 Beta

## فارسی

**زنجیره دیگر در لایهٔ دوم گیر نمی‌کند.** ریشهٔ ایراد این بود که مسیرهای VLESS در
تشخیص نوع، «xray» شمرده نمی‌شدند و پروفایل‌های ترکیبی (مثل DNSTT + SSH) هم به‌کل از
فهرست حذف می‌شدند؛ نتیجه این بود که بعد از انتخاب لایهٔ اول، هیچ گزینه‌ای برای لایهٔ
دوم نمی‌ماند. حالا مسیر VLESS همان لایهٔ Xray است و پروفایل ترکیبی می‌تواند زنجیره را
باز کند (فقط در جایگاه اول) و لایه‌های بعدی از آن تغذیه می‌کنند.

**رنگ لهجه واقعاً اعمال می‌شود.** پیش از این انتخاب رنگ فقط منابع رابط بومی را عوض
می‌کرد و نمای اصلی که HTML است روی رنگ پیش‌فرض می‌ماند؛ به همین دلیل فقط روشن/تیره
دیده می‌شد. حالا رنگ انتخاب‌شده با مدل به پوستهٔ HTML می‌رود و هم روی لهجه و هم روی
پس‌زمینهٔ گزینه‌های انتخاب‌شده اثر می‌گذارد. هر رنگ یک نقطهٔ رنگی و نام ترجمه‌شده در
تنظیمات دارد و در حالت کنتراست بالا رنگ سیستم حاکم می‌ماند.

**اتصال رایگان روونت (فقط نسخهٔ Debug).** کلاینت Free v2 دقیقاً با همان قرارداد
اندروید پیاده شد: امضای Ed25519 روی بایت‌های دقیق کاتالوگ (شامل خط جدید پایانی)،
پاکت AES-256-GCM با AAD قرارداد، بررسی SHA-256 فایل پاکت، فهرست revoked و انقضای
۳۰روزه. در آزمون محلی، شش مورد از کاتالوگ آزمایشی هم هش و هم رمزگشایی‌شان درست بود،
هر شش مسیر وصل شدند و پنج مسیر محتوای HTTPS واقعی را رد کردند. این قابلیت **در
نسخهٔ عمومی روشن نیست**: کلیدهای آزمایشی بیرون از مخزن می‌مانند و اسکریپت بسته‌بندی
اگر آن‌ها حاضر باشند از ساخت بستهٔ عمومی امتناع می‌کند. کلید ثابت آزمایشی جای سامانهٔ
تحویل کلید نهایی را نمی‌گیرد.

**محدودیت‌ها:** نصب‌کننده هنوز امضای Authenticode ندارد، پس SmartScreen ممکن است هشدار
بدهد. TUN و kill switch مثل قبل در این نسخه فعال نیستند.

## English

**The chain builder no longer stalls at the second layer.** A VLESS path was not
recognised as an Xray layer, and combined profiles such as DNSTT + SSH were
dropped from the pool entirely, so after the first layer there was nothing left
to choose. A VLESS path is treated as the Xray layer, and a combined profile can
open a chain (first layer only) and feed the layers after it.

**Accent colours now actually apply.** Choosing a colour used to change only the
native resources while the HTML shell kept its default, which is why only
light/dark appeared to work. The chosen accent now travels with the model and
drives both the accent and the selected-row background; each choice has a colour
dot and a translated name, and high contrast still wins.

**Rovunet Free (debug builds only).** The Free v2 client follows the same
contract as Android: Ed25519 over the exact catalog bytes including the trailing
newline, AES-256-GCM envelopes with the contract AAD, per-envelope SHA-256, the
revoked list and the 30-day expiry. Against the debug catalog, all six items
matched their hashes and decrypted, all six paths connected, and five carried a
real HTTPS payload. This is **disabled in the public build**: the debug keys stay
outside the repository and the packaging script refuses to build a public package
while they are present. A fixed debug key is not a substitute for the final key
delivery system.

**Limits:** the installer is not Authenticode-signed, so SmartScreen may warn.
TUN and the kill switch remain inactive in this consumer beta.
