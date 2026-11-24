# Rovunet 0.3.0-alpha23 Full · code57 · Public alpha release

## فارسی

آلفا۲۳ جای alpha22 / code49 را روی دستگاه‌های نصب‌شده می‌گیرد: همان بستهٔ `app.veyra.client`، همان امضای تولیدی و کد نسخهٔ ۵۷. پس بدون حذف اپ یا پاک‌کردن داده، مثل یک به‌روزرسانی عادی نصب می‌شود و پروفایل‌ها، تنظیمات و داده‌های رمزشده می‌مانند. این نسخه با R8 کوچک‌سازی شده و روانی پیمایش بیلدهای release قبلی را برمی‌گرداند. علت کندی گزارش‌شده، نوع بیلد بود نه کد لیست: نسخهٔ کند یک بیلد debug بود و Compose در حالت debug بدون R8 چند برابر کندتر اجرا می‌شود.

سه اصلاح کد هم کار هر فریم را کم کرده‌اند: آمار زندهٔ ترافیک از state اصلی رابط جدا شد و فقط نوار پایین آن را می‌خواند؛ ساعت اندازه‌گیری از مرحلهٔ گروه‌بندی لیست بیرون رفت؛ و ساعت فقط وقتی لیست ساکن است جلو می‌رود.

متن‌های بلند دوباره داخل کادرشان می‌چرخند: نام کانفیگ، مقصد، خط پروتکل/وضعیت، نتیجهٔ تست و نام و زیرنویس گروه. چرخش هنگام پرتاب سریع لیست خودش را متوقف می‌کند تا پیمایش سنگین نشود. پرش صفحهٔ انتخاب زبان در اولین ورود هم رفع شد: فراخوانی `recreate()` حذف شد و سه مرحلهٔ راه‌اندازی با Crossfade جابه‌جا می‌شوند؛ درخواست مجوز اعلان هم ۶۰۰ میلی‌ثانیه عقب افتاد تا روی فریم اول صفحهٔ اصلی نیفتد.

در مسیر دانلود آپدیت، نقطهٔ بازگشت (checkpoint) قبلاً هر ۲ مگابایت کل فایل نیمه‌کارهٔ ۱۵۰ مگابایتی را fsync می‌کرد و در دانلود چهاربخشی، چهار رشتهٔ دانلود پشت هم منتظر می‌ماندند؛ حدود هفتاد بار در هر APK. اکنون checkpoint درشت‌تر است: حداقل ۲۴ مگابایت دادهٔ تازه و حداقل ۱۰ ثانیه فاصله، با یک راه فرار سخت در ۹۶ مگابایت. اثر این اصلاح از به‌روزرسانی بعدیِ همین نسخه دیده می‌شود، چون بیلد نصب‌شدهٔ alpha22 هنوز کد قدیمی را دارد.

زیرساخت Free v2: نشانی رسمی کاتالوگ و کلید عمومی پین‌شدهٔ Ed25519 در بیلد قرار گرفته‌اند و کلاینت اکنون شکل خام ۳۲ بایتی کلید را هم می‌پذیرد. کاتالوگ منتشرشده پیش از این بیلد به‌صورت آفلاین بررسی شد: امضای جداگانه روی بایت‌های دقیق کاتالوگ معتبر است، SHA-256 همهٔ شش پاکت با مقدار امضاشده یکی است، و همهٔ شش پاکت با AAD مقرر به لینک‌های واقعی `rovunet://` رمزگشایی می‌شوند. کلید AES دیباگ به‌عمد در این بیلد نیست، پس Free v2 رسمی تا وقتی کارگزار کلید تحت کنترل مالک و کلیدهای نهایی ساخته شوند غیرفعال می‌ماند.

## English

Alpha23 replaces alpha22 / code49 on installed devices: same `app.veyra.client` package, same production signer and version code 57. It installs as a normal update without uninstalling or clearing data, keeping profiles, settings and encrypted data. The build is R8-minified and restores the scrolling smoothness of earlier release builds. The reported slowdown was the build type rather than the list code: the slow candidate was a debug build, and Compose in debug mode runs several times slower without R8.

Three code changes also cut per-frame work: live traffic counters moved off the main UI state and are read only by the bottom status strip; the measurement clock left the list's grouping step; and the clock advances only while the list is idle.

Overflowing text scrolls inside its box again for the config name, destination, protocol/status line, test result, and the group name and subtitle, and pauses itself while the list is flung so it cannot weigh down scrolling. The first-launch language screen no longer flashes: `recreate()` is gone, the three startup stages crossfade, and the notification permission request waits 600 ms so it does not land on the first frame of the main screen.

In the update download path, the resume checkpoint used to fsync the whole 150 MB part file every 2 MiB, serialising all four segment threads roughly seventy times per APK. Checkpoints are now coarse: at least 24 MiB of new data and at least ten seconds apart, with a hard escape at 96 MiB. The effect appears from the next update performed by this version, because the installed alpha22 build still runs the old code.

Free v2 infrastructure: the official catalog address and a pinned Ed25519 public key are compiled in, and the client now accepts the raw 32-byte key form. The published catalog was verified offline before this build: the detached signature over the exact catalog bytes is valid, all six envelopes match their signed SHA-256, and all six decrypt under the prescribed AAD to real `rovunet://` links. The debug AES key is deliberately absent, so official Free v2 stays disabled until the owner-controlled key broker and final keys exist.

## Русский

Alpha23 заменяет alpha22 / код49 на установленных устройствах: тот же пакет `app.veyra.client`, та же производственная подпись, version code 57. Ставится как обычное обновление, без удаления приложения и данных; профили, настройки и зашифрованные данные сохраняются. Сборка минифицирована R8 и возвращает плавность прокрутки прежних release-сборок. Причина замедления — тип сборки, а не код списка: медленный кандидат был debug-сборкой, а Compose в режиме debug без R8 работает в несколько раз медленнее.

Три правки кода также снизили нагрузку на кадр: счётчики трафика вынесены из основного UI-состояния и читаются только нижней полосой статуса; часы измерений больше не читаются на этапе группировки списка; часы идут только когда список неподвижен.

Длинный текст снова прокручивается внутри своего поля: имя конфига, адрес, строка протокола/статуса, результат теста, имя и подпись группы; при быстром пролистывании прокрутка останавливается, чтобы не утяжелять скролл. Экран выбора языка при первом запуске больше не мигает: `recreate()` удалён, три стадии запуска сменяются через Crossfade, а запрос разрешения на уведомления отложен на 600 мс. В загрузке обновлений контрольная точка раньше синхронизировала весь 150-мегабайтный файл каждые 2 МиБ, из-за чего четыре потока ждали друг друга около семидесяти раз на APK; теперь шаг — не менее 24 МиБ и не чаще раза в 10 секунд, с жёстким выходом на 96 МиБ. Эффект проявится начиная со следующего обновления, выполненного этой версией.

Адрес официального каталога Free v2 и закреплённый открытый ключ Ed25519 вкомпилированы; клиент принимает и «сырую» 32-байтную форму ключа. Опубликованный каталог проверен офлайн до этой сборки: отдельная подпись по точным байтам каталога действительна, SHA-256 всех шести конвертов совпадает с подписанным, и все шесть расшифровываются с предписанным AAD в реальные ссылки `rovunet://`. Отладочного AES-ключа в сборке намеренно нет, поэтому официальный Free v2 остаётся отключённым до появления контролируемого владельцем брокера ключей и финальных ключей.

## 简体中文

Alpha23 在已安装设备上取代 alpha22 / 代码49：同一 `app.veyra.client` 包名、同一生产签名、版本代码 57。作为普通更新安装，无需卸载或清除数据，配置、设置和加密数据都会保留。本构建使用 R8 压缩，恢复了此前 release 构建的滚动流畅度。变慢的原因是构建类型而非列表代码：慢的候选版是 debug 构建，Compose 在 debug 模式下不使用 R8，运行慢数倍。

三处代码改动还减少了每帧工作量：实时流量计数移出主界面状态，仅底部状态栏读取；测量时钟不再在列表分组步骤中读取；时钟仅在列表静止时推进。

溢出的文字重新在自己的框内滚动：配置名称、目标地址、协议/状态行、测试结果、分组名称与副标题；快速滑动列表时会自动暂停，避免拖慢滚动。首次启动的语言选择页不再闪烁：已删除 `recreate()`，三个启动阶段使用 Crossfade 切换，通知权限申请延后 600 毫秒。

更新下载中的断点检查原先每 2 MiB 就对整个 150 MB 文件执行 fsync，使四个分段线程每次 APK 大约互相等待七十次；现在至少每 24 MiB 且每 10 秒一次，并在 96 MiB 处强制写入。该效果从使用此版本执行的下一次更新开始显现，因为手机上已安装的 alpha22 仍运行旧代码。

官方 Free v2 目录地址和固定的 Ed25519 公钥已内置，客户端也接受原始 32 字节密钥形式。发布目录在本次构建前已离线校验：对目录精确字节的分离签名有效，六个信封的 SHA-256 与签名值一致，且六个信封都能以规定 AAD 解密为真实的 `rovunet://` 链接。构建中有意不含调试 AES 密钥，因此在所有者控制的密钥代理和最终密钥就绪之前，官方 Free v2 保持禁用。

## Identity and evidence

- Package `app.veyra.client`; version `0.3.0-alpha23`; versionCode `57`; minSdk24, targetSdk35, compileSdk36; Full Release with R8 minification.
- Published asset: `Rovunet-v0.3.0-alpha23-code57-Full.apk`, 161,238,037 bytes (153.8 MiB), single universal Full APK covering `arm64-v8a`, `armeabi-v7a` and `x86_64`. No ABI-specific or Lite asset is published with this release.
- APK SHA256: `2582abe92288fd575d10befe3d178bb0ab21d33a2ae9bc84d27b8c1e72c17053`. The published asset was downloaded back from GitHub and re-hashed; the value matches, and it matches the digest GitHub reports for the uploaded asset.
- Production certificate SHA256: `1e40bd201489a8209043e346255772823d9d52ee21b468bdef2c152d239bc416`; v2/v3 signatures; identical to alpha22, so the update path is preserved.
- Build provenance: this APK was built locally from the project source with the production keystore, not by a CI workflow, and no CI run is claimed for it. The app source published in this repository is older than the alpha23 tree, so this note does not claim a source commit for the build and the previous release's Source link was removed from the READMEs rather than left pointing at older code.
- The published catalog under `feature/free-v2-encrypted-single-configs` was verified offline against the detached Ed25519 signature, per-file SHA-256 and AES-256-GCM/AAD contract before the build; the verifier ran on the exact downloaded bytes.

## Remaining acceptance and next version

The owner tested equivalent candidate code on their phone before this production build was produced from the same source, which is why the build was published. That does not establish physical-phone battery savings, overnight/Doze stability, carrier reachability, or repair of the reported panel WireGuard/Hy2 endpoints; no new device measurements are claimed for this exact APK. TUIC remains unresolved.

Open items: decide whether the alpha23 source should be published in this repository; owner-controlled Free v2 key broker and final keys, since the official catalog stays disabled until they exist; and a device test of a second update performed by this version to confirm the new download checkpoint behaviour end to end.
