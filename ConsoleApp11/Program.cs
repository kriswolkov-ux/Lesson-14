using System;
using System.Data;
using System.Data.Common;
using Npgsql;

public class Lesson14
{
    static string connString = "Host=wotacoma.ru;Port=5090;Username=students;Password=asd123qwe4ewq321dsa;Database=9/3-RPO-24/1";

    static string myConnString = "Host=wotacoma.ru;Port=5090;Username=krishleb;Password=201123;Database=krishleb";

    public static void Run()
    {
        DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);

        DbProviderFactory factory = DbProviderFactories.GetFactory("Npgsql");

        while (true)
        {
            Console.WriteLine("=== Урок 14. DataRowState ===");
            Console.WriteLine("1. Исследование состояний");
            Console.WriteLine("2. Журнал изменений");
            Console.WriteLine("3. Несколько таблиц");
            Console.WriteLine("4. Создать таблицы");
            Console.WriteLine("5. Что в базе");
            Console.WriteLine("0. Выход");
            Console.Write("Выберите действие: ");

            string choice = Console.ReadLine() ?? "";

            if (choice == "0")
            {
                break;
            }

            try
            {
                if (choice == "1")
                {
                    Task1(factory);
                }
                else if (choice == "2")
                {
                    Task2(factory);
                }
                else if (choice == "3")
                {
                    Task3(factory);
                }
                else if (choice == "4")
                {
                    CreateTables(factory);
                }
                else if (choice == "5")
                {
                    WhatIsInDb(factory);
                }
                else
                {
                    Console.WriteLine("Неверный пункт.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }

            Console.WriteLine();
        }
    }

    static void Task1(DbProviderFactory factory)
    {
        DataTable table = new DataTable();

        using (DbConnection connection = factory.CreateConnection()!)
        {
            connection.ConnectionString = connString;

            using DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT id, first_name, last_name FROM students_lesson13 ORDER BY id";

            using DbDataAdapter adapter = factory.CreateDataAdapter()!;
            adapter.SelectCommand = cmd;

            Console.WriteLine();
            Console.WriteLine("Состояние соединения до Fill(): " + connection.State);

            adapter.Fill(table);

            Console.WriteLine("Состояние соединения после Fill(): " + connection.State);
        }

        Console.WriteLine("После выхода из using соединение закрыто.");
        Console.WriteLine("Загружено студентов: " + table.Rows.Count);

        Console.WriteLine();
        Console.WriteLine("=== Исходные состояния ===");
        PrintTable(table);

        DataRow newRow = table.NewRow();
        newRow["first_name"] = "Roman";
        newRow["last_name"] = "Stepanov";

        Console.WriteLine();
        Console.WriteLine("После NewRow(): " + newRow.RowState);

        table.Rows.Add(newRow);

        Console.WriteLine("После Rows.Add(): " + newRow.RowState);

        try
        {
            object original = newRow["first_name", DataRowVersion.Original];
            Console.WriteLine("Original добавленной строки: " + original);
        }
        catch (VersionNotFoundException)
        {
            Console.WriteLine("У добавленной строки версии Original нет");
        }

        DataRow changeRow = table.Rows[0];

        Console.WriteLine();
        Console.WriteLine("До изменения: " + changeRow.RowState);

        changeRow["first_name"] = changeRow["first_name"] + " (изменён)";

        Console.WriteLine("После изменения: " + changeRow.RowState);

        DataRow deleteRow = table.Rows[1];

        Console.WriteLine();
        Console.WriteLine("До удаления: " + deleteRow.RowState);

        deleteRow.Delete();

        Console.WriteLine("После Delete(): " + deleteRow.RowState);

        Console.WriteLine("Удалённая строка (Original): ID = "
            + deleteRow["id", DataRowVersion.Original]
            + ", Имя = " + deleteRow["first_name", DataRowVersion.Original]);

        Console.WriteLine();
        Console.WriteLine("=== Состояния после изменений ===");
        PrintTable(table);

        table.RejectChanges();

        Console.WriteLine();
        Console.WriteLine("=== После RejectChanges() ===");
        PrintTable(table);

        DataRow changeRow2 = table.Rows[0];
        changeRow2["first_name"] = changeRow2["first_name"] + " (изменён)";

        DataRow deleteRow2 = table.Rows[1];
        deleteRow2.Delete();

        DataRow newRow2 = table.NewRow();
        newRow2["first_name"] = "Roman";
        newRow2["last_name"] = "Stepanov";
        table.Rows.Add(newRow2);

        Console.WriteLine();
        Console.WriteLine("Повторили изменения:");
        Console.WriteLine("Изменённая строка: " + changeRow2.RowState);
        Console.WriteLine("Удалённая строка: " + deleteRow2.RowState);
        Console.WriteLine("Добавленная строка: " + newRow2.RowState);

        table.AcceptChanges();

        Console.WriteLine();
        Console.WriteLine("=== После AcceptChanges() ===");
        PrintTable(table);
    }

    static void Task2(DbProviderFactory factory)
    {
        DataTable table = new DataTable();

        using (DbConnection connection = factory.CreateConnection()!)
        {
            connection.ConnectionString = connString;

            using DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT id, first_name, last_name FROM students_lesson13 ORDER BY id";

            using DbDataAdapter adapter = factory.CreateDataAdapter()!;
            adapter.SelectCommand = cmd;

            adapter.Fill(table);
        }

        int wasRows = table.Rows.Count;

        DataRow changeRow = table.Rows[0];
        changeRow["first_name"] = changeRow["first_name"] + " " + changeRow["last_name"];

        DataRow newRow = table.NewRow();
        newRow["first_name"] = "Мария";
        newRow["last_name"] = "Ивановко" +
            "" +
            "";
        table.Rows.Add(newRow);

        DataRow deleteRow = table.Rows[1];
        deleteRow.Delete();

        Console.WriteLine();
        Console.WriteLine("=== Журнал изменений ===");

        foreach (DataRow row in table.Rows)
        {
            if (row.RowState == DataRowState.Modified)
            {
                object before = row["first_name", DataRowVersion.Original];
                object after = row["first_name", DataRowVersion.Current];

                Console.WriteLine($"[MODIFIED] {before} → {after}");
            }
        }

        foreach (DataRow row in table.Rows)
        {
            if (row.RowState == DataRowState.Added)
            {
                object firstName = row["first_name"];
                object lastName = row["last_name"];

                Console.WriteLine($"[ADDED]    {firstName} {lastName}");
            }
        }

        foreach (DataRow row in table.Rows)
        {
            if (row.RowState == DataRowState.Deleted)
            {
                object firstName = row["first_name", DataRowVersion.Original];
                object lastName = row["last_name", DataRowVersion.Original];

                Console.WriteLine($"[DELETED]  {firstName} {lastName}");
            }
        }

        table.RejectChanges();

        int changed = 0;

        foreach (DataRow row in table.Rows)
        {
            if (row.RowState != DataRowState.Unchanged)
            {
                changed++;
            }
        }

        Console.WriteLine();
        Console.WriteLine("=== После RejectChanges() ===");
        Console.WriteLine("Строк в таблице: " + table.Rows.Count + ", было: " + wasRows);
        Console.WriteLine("Строк с изменениями: " + changed);
    }

    static void Task3(DbProviderFactory factory)
    {
        DataSet dataSet = new DataSet();

        using (DbConnection connection = factory.CreateConnection()!)
        {
            connection.ConnectionString = myConnString;

            using DbCommand cmdGroups = connection.CreateCommand();
            cmdGroups.CommandText = "SELECT id, name FROM krishleb.groups ORDER BY id";

            using DbDataAdapter adapterGroups = factory.CreateDataAdapter()!;
            adapterGroups.SelectCommand = cmdGroups;

            DataTable groups = new DataTable("Groups");
            adapterGroups.Fill(groups);

            using DbCommand cmdStudents = connection.CreateCommand();
            cmdStudents.CommandText = "SELECT id, first_name, last_name, group_id FROM krishleb.students ORDER BY id";

            using DbDataAdapter adapterStudents = factory.CreateDataAdapter()!;
            adapterStudents.SelectCommand = cmdStudents;

            DataTable students = new DataTable("Students");
            adapterStudents.Fill(students);

            dataSet.Tables.Add(groups);
            dataSet.Tables.Add(students);

            DataColumn parentColumn = dataSet.Tables["Groups"].Columns["id"];
            DataColumn childColumn = dataSet.Tables["Students"].Columns["group_id"];

            DataRelation relation = new DataRelation("GroupsStudents", parentColumn, childColumn);
            dataSet.Relations.Add(relation);
        }

        Console.WriteLine("Соединение закрыто.");

        Console.WriteLine();
        Console.WriteLine("В DataSet таблиц: " + dataSet.Tables.Count);
        Console.WriteLine("Связей: " + dataSet.Relations.Count);

        Console.WriteLine();
        PrintContent(dataSet.Tables["Groups"]);
        PrintContent(dataSet.Tables["Students"]);

        Console.WriteLine();
        Console.Write("Введите название группы: ");
        string groupName = Console.ReadLine() ?? "";

        bool found = false;

        foreach (DataRow groupRow in dataSet.Tables["Groups"].Rows)
        {
            if (groupRow["name"].ToString() == groupName)
            {
                found = true;

                Console.WriteLine();
                Console.WriteLine("Студенты группы " + groupName + ":");

                foreach (DataRow studentRow in groupRow.GetChildRows("GroupsStudents"))
                {
                    Console.WriteLine("  " + studentRow["last_name"] + " " + studentRow["first_name"]);
                }
            }
        }

        if (!found)
        {
            Console.WriteLine("Такая группа не найдена.");
        }
    }

    static void CreateTables(DbProviderFactory factory)
    {
        using DbConnection connection = factory.CreateConnection()!;
        connection.ConnectionString = myConnString;
        connection.Open();

        string[] createQueries =
        {
            "CREATE SCHEMA IF NOT EXISTS krishleb",

            "CREATE TABLE IF NOT EXISTS krishleb.groups ("
                + "id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY, "
                + "name varchar(100) NOT NULL)",

            "CREATE TABLE IF NOT EXISTS krishleb.students ("
                + "id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY, "
                + "first_name varchar(100) NOT NULL, "
                + "last_name varchar(100) NOT NULL, "
                + "group_id integer NOT NULL REFERENCES krishleb.groups(id))"
        };

        foreach (string query in createQueries)
        {
            using DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
        }

        Console.WriteLine("Таблицы Groups и Students готовы.");

        using (DbCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM krishleb.groups";

            if (Convert.ToInt64(cmd.ExecuteScalar()) == 0)
            {
                cmd.CommandText = "INSERT INTO krishleb.groups (name) VALUES "
                    + "('9/3-РПО-24/1'), ('9/3-РПО-24/2'), ('9/3-ИС-24/1')";

                cmd.ExecuteNonQuery();
                Console.WriteLine("Группы добавлены.");
            }

            cmd.CommandText = "SELECT COUNT(*) FROM krishleb.students";

            if (Convert.ToInt64(cmd.ExecuteScalar()) == 0)
            {
                cmd.CommandText = "INSERT INTO krishleb.students (first_name, last_name, group_id) VALUES "
                    + "('Roman', 'Stepanov', 1), "
                    + "('Иван', 'Петренко', 1), "
                    + "('Мария', 'Ивановко', 1), "
                    + "('Алексей', 'Смирновко', 2), "
                    + "('Ольга', 'Кузнецовко', 2), "
                    + "('Дмитрий', 'Волковко', 3)";

                cmd.ExecuteNonQuery();
                Console.WriteLine("Студенты добавлены.");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Что теперь лежит в таблицах:");

        using (DbCommand cmdGroups = connection.CreateCommand())
        {
            cmdGroups.CommandText = "SELECT id, name FROM krishleb.groups ORDER BY id";

            using DbDataReader reader = cmdGroups.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine("  groups: " + reader["id"] + " | " + reader["name"]);
            }
        }

        using (DbCommand cmdStudents = connection.CreateCommand())
        {
            cmdStudents.CommandText = "SELECT id, first_name, last_name, group_id FROM krishleb.students ORDER BY id";

            using DbDataReader reader = cmdStudents.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine("  students: " + reader["id"] + " | " + reader["last_name"]
                    + " " + reader["first_name"] + " | группа " + reader["group_id"]);
            }
        }

        connection.Close();
    }

    static void WhatIsInDb(DbProviderFactory factory)
    {
        Console.WriteLine();
        Console.WriteLine("1 - своя база (krishleb)");
        Console.WriteLine("2 - общая база (9/3-RPO-24/1)");
        Console.Write("Какую смотреть? ");

        string choice = Console.ReadLine() ?? "";

        string connection = choice == "2" ? connString : myConnString;

        using DbConnection db = factory.CreateConnection()!;
        db.ConnectionString = connection;
        db.Open();

        using (DbCommand cmd = db.CreateCommand())
        {
            cmd.CommandText = "SELECT current_user AS usr, current_database() AS db";

            using DbDataReader reader = cmd.ExecuteReader();
            reader.Read();

            Console.WriteLine();
            Console.WriteLine("Пользователь: " + reader["usr"]);
            Console.WriteLine("База: " + reader["db"]);
        }

        Console.WriteLine();
        Console.WriteLine("Таблицы:");

        using (DbCommand cmd = db.CreateCommand())
        {
            cmd.CommandText = "SELECT schemaname, tablename, tableowner FROM pg_tables "
                + "WHERE schemaname NOT IN ('pg_catalog', 'information_schema') "
                + "ORDER BY schemaname, tablename";

            using DbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine("  " + reader["schemaname"] + "." + reader["tablename"]
                    + "   владелец: " + reader["tableowner"]);
            }
        }

        Console.Write("Введите имя таблицы (Enter — пропустить): ");
        string table = Console.ReadLine() ?? "";

        if (table == "")
        {
            return;
        }

        Console.WriteLine();

        try
        {
            using DbCommand cmd = db.CreateCommand();
            cmd.CommandText = "SELECT * FROM " + table + " LIMIT 20";

            using DbDataReader reader = cmd.ExecuteReader();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                Console.Write(reader.GetName(i) + "\t");
            }

            Console.WriteLine();

            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.Write(reader.GetValue(i) + "\t");
                }

                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Не удалось прочитать таблицу: " + ex.Message);
        }
    }

    static void PrintTable(DataTable table)
    {
        foreach (DataRow row in table.Rows)
        {
            if (row.RowState == DataRowState.Deleted)
            {
                Console.WriteLine("ID: " + row["id", DataRowVersion.Original]
                    + ", Имя: " + row["first_name", DataRowVersion.Original]
                    + ", Фамилия: " + row["last_name", DataRowVersion.Original]
                    + ", Состояние: " + row.RowState);
            }
            else
            {
                Console.WriteLine("ID: " + row["id"]
                    + ", Имя: " + row["first_name"]
                    + ", Фамилия: " + row["last_name"]
                    + ", Состояние: " + row.RowState);
            }
        }
    }

    static void PrintContent(DataTable table)
    {
        Console.WriteLine();
        Console.WriteLine("Таблица " + table.TableName + " (строк: " + table.Rows.Count + "):");

        foreach (DataColumn column in table.Columns)
        {
            Console.Write(column.ColumnName + "\t");
        }

        Console.WriteLine();

        foreach (DataRow row in table.Rows)
        {
            foreach (DataColumn column in table.Columns)
            {
                Console.Write(row[column] + "\t");
            }

            Console.WriteLine();
        }
    }
}

class Program
{
    static void Main()
    {
        Lesson14.Run();
    }
}
