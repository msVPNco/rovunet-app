# Rovunet 0.3.0-alpha23.1 Full · code58 · Public release

## فارسی

همان بیلد alpha23 با دو تغییر: **اتصال رایگان رووُنت فعال است** و **مسیر به‌روزرسانی
درست کار می‌کند**. در alpha23 کلید AES عمداً کامپایل نشده بود، پس مسیر Free v2 همیشه
با `ROVUNET_FREE_UNVERIFIED` («اعتبار فهرست تأیید نشد») تمام می‌شد — حتی وقتی فهرست،
امضا و کلید عمومی سالم بودند. اکنون کلید در بیلد است و پیش از ساخت، زنجیره آفلاین
بررسی شد: امضای Ed25519 معتبر، SHA-256 هر شش پیکربندی مطابق مقدار امضاشده، و هر شش
قفل با AAD مقرر باز می‌شود (۶ از ۶). فهرست تا ۲۰۲۶-۱۲-۲۳ معتبر است و سهمیهٔ ۵۰ GiB
مشترک است.

**هشدار:** کلید AES همراه APK منتشر شده است؛ هر کسی این فایل را داشته باشد می‌تواند
لینک‌های فهرست را ببیند. برای انتشار بلندمدت، قرارداد Free v2 کارگزار کلید تحت کنترل
مالک و کلیدهای نهایی را می‌خواهد.

**به‌روزرسانی:** پنجرهٔ «نسخهٔ جدید موجود است» هنگام باز شدن اپ خودش می‌آید؛ بررسی
تنظیمات که قبلاً هیچ دکمه‌ای نشان نمی‌داد حالا پنجرهٔ دانلود درون‌برنامه‌ای را باز
می‌کند؛ فاصلهٔ بررسی ۶ ساعت است و زمان بررسی فقط پس از یک بررسی موفق ذخیره می‌شود.

هیچ تغییر دیگری در لیست، پروتکل‌ها یا ظاهر نیست. نصب روی alpha23 / code57 و هر نسخهٔ
عمومی قدیمی‌تر بدون حذف انجام می‌شود.

## English

Same alpha23 build with the Rovunet free connection enabled and a working update path.
alpha23 shipped without the AES key, so Free v2 always ended in
`ROVUNET_FREE_UNVERIFIED`; the catalog, signature and public key were healthy. The key is
now compiled in, and the chain was verified offline before the build: valid Ed25519
signature, all six config hashes match, all six envelopes authenticate under the
prescribed AAD. The catalog is valid until 2026-12-23 and its 50 GiB quota is shared.

**Security notice:** the AES key ships inside the APK, so anyone with the file can read the
catalog links. For long-term distribution the Free v2 contract calls for an
owner-controlled key broker and final keys.

**Updates:** the "new version available" dialog appears on launch by itself; the Settings
check, which previously showed nothing when it found a release, now opens the in-app
download dialog; the check interval is 6 hours and the last-check time is only recorded
after a completed check.

Nothing else changed in the list, protocols or appearance. Installs over alpha23 / code57
and any older public build without uninstalling.

## Identity and evidence

- Package `app.veyra.client`; version `0.3.0-alpha23.1`; versionCode `58`; minSdk24, targetSdk35, compileSdk36; Full Release with R8 minification.
- Asset `Rovunet-v0.3.0-alpha23.1-code58-Full.apk`, 161,239,429 bytes, universal (`arm64-v8a`, `armeabi-v7a`, `x86_64`).
- APK SHA256: `3a7a0f0909a9f767a27e877e13b67af063d99fd3a548cccf08fcaca2d8082b8d`; downloaded back from the release and re-hashed, matching the GitHub asset digest.
- Production certificate SHA256: `1e40bd201489a8209043e346255772823d9d52ee21b468bdef2c152d239bc416`, unchanged from alpha22 and alpha23.
- Built locally with the production keystore from the same source tree as alpha23 plus the update-path fixes; no CI run is claimed.
- The AES key and pinned Ed25519 public key are compile-time inputs from the private `local.properties`, which stays out of source control.
