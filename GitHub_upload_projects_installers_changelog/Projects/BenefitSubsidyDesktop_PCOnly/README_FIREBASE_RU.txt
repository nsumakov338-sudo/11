ПОДКЛЮЧЕНИЕ FIREBASE К ГОТОВОМУ ПК-ПРИЛОЖЕНИЮ

В этой версии Firebase уже встроен в программу.
Ничего в коде менять не нужно. Нужно только заполнить настройки через кнопку в приложении.

1. В Firebase Console создай проект.
2. Открой Build → Authentication → Sign-in method.
3. Включи Email/Password.
4. Открой Authentication → Users → Add user.
   Рекомендуемый пользователь для ПК-приложения:
   operator@test.ru / 123456
5. Скопируй UID оператора из Authentication → Users.
6. Открой Realtime Database → Data → Import JSON.
   Импортируй файл:
   Firebase/firebase_database_import.json
7. В Realtime Database → Data замени OPERATOR_UID_HERE на настоящий UID оператора.
   Также замени MANAGER_UID_HERE и ADMIN_UID_HERE, если создавал менеджера и администратора.
8. Открой Realtime Database → Rules.
   Для первой проверки можно поставить простые правила:

{
  "rules": {
    ".read": "auth != null",
    ".write": "auth != null"
  }
}

   После проверки можно использовать более строгие правила из файла:
   Firebase/firebase_rules.json

9. Запусти программу.
10. Нажми слева кнопку «Настройки Firebase».
11. Включи галочку «Использовать настоящую Firebase Database вместо демо-облака».
12. Заполни:
   - Firebase Database URL
   - Web API Key
   - Email оператора
   - Пароль оператора
13. Нажми «Сохранить».
14. Нажми «Синхронизировать».

Если всё сделано правильно, заявки появятся в Firebase:
Realtime Database → Data → applications

ГДЕ ВЗЯТЬ FIREBASE DATABASE URL
Realtime Database → Data → ссылка сверху.
Пример:
https://project-default-rtdb.europe-west1.firebasedatabase.app

ГДЕ ВЗЯТЬ WEB API KEY
Project settings → General → Web API Key.

ВАЖНО
Без твоего Firebase Database URL и Web API Key я не могу заранее полностью привязать программу к твоему аккаунту Firebase.
Но код подключения уже готов: программа сама авторизуется, получает idToken, читает и записывает заявки в Firebase Realtime Database.

КАК БУДЕТ РАБОТАТЬ С ТЕЛЕФОНОМ
Будущее мобильное приложение должно использовать тот же Firebase Database URL и тот же путь applications.
Тогда изменения с ПК будут появляться на телефоне, а изменения с телефона будут приходить в ПК после синхронизации.
