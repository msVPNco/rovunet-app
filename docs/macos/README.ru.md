[English](README.md) · [فارسی](README.fa.md) · [Русский](README.ru.md) · [简体中文](README.zh-CN.md)

# Rovunet macOS 0.1.8 Beta

**Pre-release · internal build 0.1.7.20 · macOS 15+**

| Mac | Установщик и контрольная сумма | Проверки |
| --- | --- | --- |
| Apple Silicon · arm64 | [PKG](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-AppleSilicon-arm64.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | Установка, интерфейс и повседневный TUN подтверждены |
| Intel · x64 | [PKG · NOT-TESTED](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-Intel-x64-NOT-TESTED.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | **НЕ ПРОВЕРЕН на физическом Intel Mac** |

Предварительная версия для **macOS 15+**; отдельные установщики Apple Silicon и Intel. Версия **0.1.8 Beta**, внутренняя сборка **0.1.7.20**.

Результаты DNS Scanner стали компактнее, технические подробности раскрываются стрелкой. Статусы DNS, туннеля и HTTPS, ход проверки и ошибки остаются видимыми; раскрытие не выбирает DNS. Быстрая проверка отделена от точного HTTPS-теста. Для поддерживаемых UDP-протоколов используются аутентифицированные ответы протокола; один успешный пинг не доказывает передачу данных. Сохранены предыдущие исправления интерфейса, настроек и совместимости подписок панели.

На Apple Silicon приложение установлено и запущено; **492 файла** совпали с пакетом. **2139 проверок интерфейса, без ошибок**. Владелец подтвердил внешний вид и повседневную работу TUN. В предыдущей сборке 19 полное тело HTTPS прошло через **18/20 быстрых путей** и затем **8/8 DNS-путей**, включая SSH, с TCP-резолверами. Это не полный повтор тестов сборки 20. По отзыву владельца, в Канаде с Panel 2.0.2 работает всё, кроме TUIC; его сбой в нескольких клиентах требует проверки панели/сервера. Старый результат WireGuard на другом сервере рассматривается отдельно.

Перед установкой отключитесь и закройте приложение через Command-Q. Выберите PKG своего процессора; сохраните профили и Keychain. Подпись приложения локальная **ad hoc; Apple Developer ID и notarization отсутствуют**. Возможны предупреждения macOS; не отключайте Gatekeeper глобально. **Intel на реальном оборудовании не проверен.** Восстановление после принудительного завершения, DNS/маршрутов, сон/пробуждение, kill-switch и длительное энергопотребление ещё не прошли полную приёмку. Независимое Smart-сравнение при глобальном TUN ограничено. Доставку доверенного сертификата Slipstream на текущей панели нужно перепроверить; TLS/pin не обходятся. Проверка WG/TUIC внутри HY2 не равна прямому соединению.

## Source / منشأ / Исходники / 源码

[ZIP точных исходников](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-source.zip) · [Verification](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/VERIFICATION.json) · [Release notes](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.8-beta) · [Four-language record](releases/0.1.8-beta.md)

Exact local source snapshot: `71adb8900c68fb3b97f97903877902db718f586a`. This snapshot is not an upstream-published application commit. The attached source ZIP corresponds to these binaries; GitHub's automatic source archive is not asserted to match it. All eleven native engines and the TUN helper are reused unchanged from internal 0.1.7.19. No fresh full native rebuild or exhaustive protocol acceptance is claimed. Use the macOS release channel rather than repository-wide Latest. PKG installers only; no DMG is provided.

[Main project](../../README.ru.md) · [Previous macOS preview](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.7-beta)
