# Rovunet Windows 0.1.5 Beta

## فارسی

**زنجیره حالا پله‌به‌پله ساخته می‌شود.** قبلاً هر لایه همهٔ پروفایل‌ها را نشان می‌داد و
ترکیب ناممکن فقط هنگام ذخیره رد می‌شد؛ کاربر دلیلش را نمی‌فهمید. حالا هر ردیف فقط
پروفایل‌هایی را پیشنهاد می‌دهد که در همان جایگاه مجازند — لایهٔ اول، لایهٔ دوم و… —
پس زنجیرهٔ نامعتبر اصلاً ساخته نمی‌شود.

**به‌روزرسانی داخل برنامه کامل شد.** صفحهٔ به‌روزرسانی فایل نصب را با تأیید SHA-256
دانلود می‌کند و اکنون دکمهٔ «نصب به‌روزرسانی» خودِ نصب‌کننده را اجرا می‌کند؛ لازم نیست
فایل را دستی پیدا کنید.

**رنگ لهجه.** هشت رنگ مثل اندروید: بنفش، فیروزه‌ای، آبی، ارغوانی، گلی، کهربایی، سبز و
سرمه‌ای. از تنظیمات ← نمایش.

**زبان‌ها از منوی سه‌نقطه.** پرچم‌ها از نوار بالا برداشته شدند و فهرست زبان‌ها به منوی
سه‌نقطه رفت. زبان‌های فعلی: فارسی، انگلیسی، روسی، چینی، عربی، ترکی، اردو، اسپانیایی و
فرانسوی. زبان‌هایی که فایل ترجمهٔ کامل ندارند در فهرست نمایش داده نمی‌شوند تا هیچ‌جا
متن انگلیسیِ ناخواسته ظاهر نشود.

**نوار رنگ سرعت روی هر مسیر.** مثل اندروید: سبز (سریع‌ترین)، زیتونی، کهربایی و نارنجی.

**نصب روی همهٔ نسخه‌های ویندوز ۱۰ و ۱۱.** شرط نسخه در برنامه، ایجنت و نصب‌کننده از
build 19045 (ویندوز ۱۰ نسخهٔ 22H2) به build 17763 (ویندوز ۱۰ نسخهٔ 1809) کاهش یافت؛
روی ویندوز ۱۰های قدیمی‌تر پیام «نسخه پشتیبانی نمی‌شود» می‌آمد و نصب‌کننده بسته می‌شد.

**محدودیت‌ها:** نصب‌کننده هنوز امضای Authenticode ندارد، پس SmartScreen ممکن است هشدار
بدهد. TUN و kill switch مثل قبل در این نسخه فعال نیستند.

## English

**The chain builder is now stepwise.** Each dropdown lists only the profiles valid at that
position, so an impossible chain cannot be assembled and then rejected at save time.

**In-app update is complete.** The update page verifies the installer's SHA-256 and now
offers an “Install update” button that launches the installer itself.

**Accent colours.** Eight choices, matching Android, under Settings → Appearance.

**Languages moved to the three-dot menu**, and the header flags are gone. Shipped:
Persian, English, Russian, Chinese, Arabic, Turkish, Urdu, Spanish, French. A language
without a complete locale file is not listed, so no screen silently shows English.

**Speed stripe per path row**, using the same four tiers and colours as Android.

**Installable on every Windows 10 and 11 build.** The version floor in the app, agent and
installer moved from build 19045 (Windows 10 22H2) to build 17763 (Windows 10 1809).

**Limits:** the installer is not Authenticode-signed, so SmartScreen may warn. TUN and the
kill switch remain inactive in this consumer beta.
