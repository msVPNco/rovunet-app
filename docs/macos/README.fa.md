[English](README.md) · [فارسی](README.fa.md) · [Русский](README.ru.md) · [简体中文](README.zh-CN.md)

# Rovunet macOS 0.1.8 Beta

**Pre-release · internal build 0.1.7.20 · macOS 15+**

| نوع مک | نصب‌کننده و هش | وضعیت آزمون |
| --- | --- | --- |
| Apple Silicon · arm64 | [PKG](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-AppleSilicon-arm64.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | نصب و رابط تأیید شده؛ TUN روزمره با تأیید مالک |
| Intel · x64 | [PKG · NOT-TESTED](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-Intel-x64-NOT-TESTED.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | **روی سخت‌افزار اینتل واقعی تست نشده** |

نسخهٔ آزمایشی macOS برای **macOS 15 و بالاتر**، با بسته‌های جداگانهٔ Apple Silicon و Intel. نسخهٔ عمومی **۰٫۱٫۸ بتا** و شمارهٔ ساخت داخلی **۰٫۱٫۷٫۲۰** است.

### بازکردن بسته بدون امضای اپل

این نسخه امضای محلی ad hoc دارد و فاقد Developer ID و notarization اپل است. در نتیجه ممکن است macOS پیام «Apple could not verify…» یا «Not Opened» نشان دهد.

1. بستهٔ متناسب با پردازنده را فقط از [ریلیز رسمی Rovunet](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.8-beta) بگیرید و هش آن را با `SHA256SUMS.txt` همان ریلیز تطبیق دهید.
2. PKG را باز کنید؛ اگر هشدار تأییدنشدن اپل ظاهر شد، **Done** را بزنید.
3. وارد **Apple menu → System Settings → Privacy & Security** شوید و پایین صفحه، بخش **Security** را پیدا کنید.
4. کنار پیام مسدودشدن **همین فایل Rovunet**، **Open Anyway** را بزنید.
5. هشدار بعدی را بخوانید؛ فقط اگر به منبع فایل اعتماد دارید، **خودتان Open و تأیید Touch ID یا رمز مک را انجام دهید**.
6. نصب‌کننده را ادامه دهید. هنگام ارتقا، داده‌ها و Keychain را حذف نکنید. اگر پس از نصب خود برنامهٔ Rovunet هم مسدود شد، همین مراحل را برای آن تکرار کنید.

اگر Open Anyway دیده نمی‌شود، دوباره تلاش کنید همان فایل را باز کنید و سپس به این صفحه برگردید؛ در مک تحت مدیریت سازمان، سیاست مدیر سیستم می‌تواند مانع شود. Gatekeeper را سراسری خاموش نکنید؛ فرمان حذف quarantine یا فعال‌کردن «Anywhere» لازم نیست. این راهنما مخصوص هشدار تأییدنشدن توسعه‌دهنده است؛ برای پیام صریح آسیب‌رسان‌بودن یا خراب‌بودن فایل، نصب را ادامه ندهید و فایل/هش را بررسی کنید.

[راهنمای رسمی اپل](https://support.apple.com/en-us/102445)

### تغییرات

- نتایج DNS Scanner کوتاه‌تر شده‌اند؛ جزئیات فنی با فلش باز و بسته می‌شوند. وضعیت DNS، تونل، HTTPS و خطاها همچنان دیده می‌شوند؛ بازکردن جزئیات، resolver را انتخاب نمی‌کند.
- تست سریع و تست دقیق HTTPS جدا هستند؛ برای پروتکل‌های UDP پشتیبانی‌شده، تست سریع از پاسخ معتبر خود پروتکل استفاده می‌کند. پینگ موفق به‌تنهایی به معنی عبور داده نیست.
- این بسته اصلاحات قبلی رابط، تنظیمات اتصال و سازگاری اشتراک پنل را حفظ می‌کند. آخرین تغییر ساخت ۲۰ مربوط به چیدمان نتایج اسکنر است.

### وضعیت آزمون‌ها

بستهٔ Apple Silicon روی مک نصب و باز شده و ۴۹۲ فایل نصب‌شده با بسته مطابقت داشته‌اند. **۲۱۳۹ بررسی رابط، بدون شکست**؛ ظاهر و استفادهٔ روزمره از TUN توسط مالک تأیید شده‌اند. تصاویر fixture و بررسی‌های خودکار رابط، جای آزمون واقعی همهٔ شبکه‌ها را نمی‌گیرند.

در دور قبلی ساخت ۱۹، ۱۸ از ۲۰ مسیر سریع و سپس هر ۸ مسیر DNS شامل مسیرهای SSH با TCP، بدنهٔ کامل HTTPS را عبور دادند. این نتیجه، تکرار کامل آزمون پروتکل‌ها روی ساخت ۲۰ نیست. مالک در کانادا با پنل ۲٫۰٫۲ موفقیت مسیرها به‌جز TUIC را گزارش کرده؛ TUIC طبق گزارش او در چند برنامه ناموفق بوده و نیازمند بررسی پنل/سرور است. نتیجهٔ قدیمی WireGuard روی سرور دیگر به این گزارش تعمیم داده نمی‌شود.

### نصب و محدودیت‌ها

اتصال را قطع و برنامه را با Command-Q ببندید؛ سپس PKG متناسب با پردازنده را نصب کنید. برای به‌روزرسانی، پروفایل‌ها و اقلام Keychain را حذف نکنید. بستهٔ برنامه امضای محلی ad hoc دارد؛ **Developer ID اپل و notarization ندارد**. ممکن است macOS هنگام نصب یا اجرا هشدار امنیتی نشان دهد؛ حفاظت سراسری Gatekeeper را خاموش نکنید.

**Intel روی سخت‌افزار واقعی آزمایش نشده است.** آزمون قطع اجباری، بازیابی DNS/مسیر شبکه، خواب و بیداری، kill-switch و مصرف طولانی انرژی هنوز پذیرش کامل ندارند. Smart comparison مستقل هنگام TUN سراسری نیز محدودیت دارد. وضعیت اعتماد Slipstream روی پنل فعلی نیازمند تأیید مجدد است؛ خطای TLS یا تغییر pin نباید خودکار نادیده گرفته شود. موفقیت تشخیصی WG/TUIC داخل HY2، موفقیت اتصال مستقیم محسوب نمی‌شود.

## Source / منشأ / Исходники / 源码

[ZIP سورس دقیق](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-source.zip) · [Verification](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/VERIFICATION.json) · [Release notes](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.8-beta) · [Four-language record](releases/0.1.8-beta.md)

Exact local source snapshot: `71adb8900c68fb3b97f97903877902db718f586a`. This snapshot is not an upstream-published application commit. The attached source ZIP corresponds to these binaries; GitHub's automatic source archive is not asserted to match it. All eleven native engines and the TUN helper are reused unchanged from internal 0.1.7.19. No fresh full native rebuild or exhaustive protocol acceptance is claimed. Use the macOS release channel rather than repository-wide Latest. PKG installers only; no DMG is provided.

[Main project](../../README.fa.md) · [Previous macOS preview](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.7-beta)
