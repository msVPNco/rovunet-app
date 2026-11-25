# Rovunet Windows 0.2.0 · x64

The first public Windows build that includes the official Rovunet Free source, the lifted dark palette and the large-collection performance work.

## What is new

- **Rovunet Free source.** The official source is in the One-click Public Sources list. The signed catalog is verified with Ed25519, every config's SHA-256 is checked, and each envelope is decrypted with AES-256-GCM before its link joins the normal tunnel path. The six paths are offered in random group-first order and the 50 GiB donation quota stays shared per donation group.
- **Dark theme.** The dark palette is a deep blue-slate instead of near-black, so cards, rows and panels keep their depth. AMOLED remains true black by design.
- **Large collections.** Tapping or opening a collection with hundreds of paths no longer rebuilds the page or waits on the encrypted store: a 500-path group opens in about 70 ms in the fixture, and a theme switch with 500 paths takes about 300 ms.
- **Installer.** Same compact, non-scrolling setup page, now with a Windows 10 1809 (build 17763) floor that matches the app and the agent. Every Windows 10 build from 1809 through 22H2 and every Windows 11 build is supported, x64 only.

## Install

Download **Rovunet-Windows-0.2.0-x64-Setup.exe** and verify it against `SHA256SUMS.txt`. The installer is not Authenticode-signed. Close the previous app from the tray menu before upgrading; your profiles, settings and selected path are kept.

---

# Rovunet ویندوز 0.2.0 · x64

اولین نسخهٔ عمومی ویندوز که منبع رسمی روونت رایگان، پالت تیرهٔ روشن‌تر و اصلاح سرعت مجموعه‌های بزرگ در آن هست.

## تغییرات

- **منبع روونت رایگان:** در فهرست منابع عمومی با همان دکمهٔ یک‌مرحله‌ای. کاتالوگ امضاشده با Ed25519 بررسی می‌شود، هش SHA-256 هر کانفیگ کنترل می‌شود و هر پاکت با AES-256-GCM رمزگشایی می‌شود؛ سپس مسیر به همان مسیر عادی تونل وصل می‌شود. شش مسیر به‌صورت تصادفی و گروه‌به‌گروه پیشنهاد می‌شوند و سهمیهٔ ۵۰ گیگابایتی بین مسیرهای یک اهدا مشترک می‌ماند.
- **تم تیره:** به‌جای تقریباً سیاه، آبی‌خاکستری عمیق است و کارت‌ها و ردیف‌ها عمق خود را نگه می‌دارند. AMOLED عمداً سیاه مطلق ماند.
- **مجموعه‌های بزرگ:** باز کردن مجموعه‌ای با صدها مسیر دیگر کل صفحه را بازنمی‌سازد و منتظر ذخیرهٔ رمزشده نمی‌ماند؛ در آزمون، مجموعهٔ ۵۰۰ مسیری حدود ۷۰ میلی‌ثانیه و عوض کردن تم حدود ۳۰۰ میلی‌ثانیه است.
- **نصاب:** همان صفحهٔ جمع‌وجور و بی‌اسکرول، حالا با کف ویندوز ۱۰ نسخهٔ 1809 (build 17763) هم‌راستا با برنامه و Agent. همهٔ نسخه‌های ویندوز ۱۰ از 1809 تا 22H2 و همهٔ نسخه‌های ویندوز ۱۱ پشتیبانی می‌شوند؛ فقط ۶۴ بیتی.

## نصب

فایل **Rovunet-Windows-0.2.0-x64-Setup.exe** را بگیرید و با `SHA256SUMS.txt` تطبیق دهید. نصب‌کننده امضای Authenticode ندارد. پیش از ارتقا، برنامهٔ قبلی را از منوی کنار ساعت ببندید؛ مسیرها، تنظیمات و انتخاب قبلی حفظ می‌شوند.
