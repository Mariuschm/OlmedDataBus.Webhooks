/*
    Poprawne zmiany:
    1) Ujednolicono formatowanie i wielkość słów kluczowych SQL.
    2) Dodano średniki kończące instrukcje.
    3) Ujednolicono zapisy nazw schematu, tabeli i użytkowników.
*/

GRANT INSERT, UPDATE, SELECT ON SCHEMA::[ProRws] TO [CDNStdADOOlmed];
GRANT INSERT, UPDATE, SELECT ON SCHEMA::[ProRws] TO [CDNStdOlmed];

GRANT INSERT, UPDATE, SELECT ON [ProRws].[Queue] TO [CDNStdOlmed];
GRANT INSERT, UPDATE, SELECT ON [ProRws].[Queue] TO [CDNStdADOOlmed];
