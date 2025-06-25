URL Shortner

This application is integrated with MySQL database so the data will persist, even when the application is restarted.
Make sure to follow the steps to create the columns on the DB

-Update the appsettings.json with the connectionString.
-Create the database used on the connectionString on MySQL.
-Open PowerShell, access the infrastructure project. Create the migration and update the database.
*******
AUTH
-POST /register (Public)
The databse has no users, so when starting the application make sure to create one.

-POST /login (Public)
Use your credentials to login. Without loging in, it will not be possible to access the endpoint that require authentication.

URL
-POST /shorten (Authenticated)
Create a shortcode from the long url provided. This shortcode will be attached to the user who created it. 
It is only allowed the creation of 5 shortcodes per user, per day.

-GET /urls (Authenticated)
Recieve a list of the URLs created by the user.

-DELETE /urls/{short_code} (Authenticated)
The user will be able to delete the shortcodes created by oneself.

-GET /{short_code} (Public)
Anyone will be able to access the website using a shortcode. Limited by 10 accesses per minute.