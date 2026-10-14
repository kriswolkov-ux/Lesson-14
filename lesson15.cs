using System;
using System.Data;
using System.Data.Common;
using Npgsql;

// тоже самое но с 4-5 заданием

public class Lesson14
{
    static string connString = "Host=wotacoma.ru;Port=5090;Username=students;Password=asd123qwe4ewq321dsa;Database=9/3-RPO-24/1";

    static string myConnString = "Host=wotacoma.ru;Port=5090;Username=krishleb;Password=201123;Database=krishleb";
// так же я понял что делал не в public...щас вроде все хорошо....вроде...
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
            Console.WriteLine("4. Фильтрация и сортировка"); //ето
            Console.WriteLine("5. Редактирование и сохранение"); // и ето
            Console.WriteLine("6. Что в базе");
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
                    States(factory);
                }
                else if (choice == "2")
                {
                    ChangeLog(factory);
                }
                else if (choice == "3")
                {
                    TwoTables(factory);
                }
                else if (choice == "4")
                {
                    FilterAndSort(factory);
                }
                else if (choice == "5")
                {
                    SaveChanges(factory);
                }
                else if (choice == "6")
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

    static DataTable LoadStudents(DbProviderFactory factory)
    {
        DataTable table = new DataTable();

        using (DbConnection connection = factory.CreateConnection()!)
        {
            connection.ConnectionString = myConnString;

            using DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT id, nickname, first_name, last_name, age FROM public.students_lesson14 ORDER BY id";

            using DbDataAdapter adapter = factory.CreateDataAdapter()!;
            adapter.SelectCommand = cmd;

            Console.WriteLine();
            Console.WriteLine("Состояние соединения до Fill(): " + connection.State);

            adapter.Fill(table);

            Console.WriteLine("Состояние соединения после Fill(): " + connection.State);
        }

        Console.WriteLine("После выхода из using соединение закрыто.");
        Console.WriteLine("Загружено студентов: " + table.Rows.Count);

        return table;
    }

    static void States(DbProviderFactory factory)
    {
        DataTable table = LoadStudents(factory);

        Console.WriteLine();
        Console.WriteLine("=== Исходные состояния ===");
        PrintTable(table);

        DataRow newRow = table.NewRow();
        newRow["nickname"] = "krishleb";
        newRow["first_name"] = "Roman";
        newRow["last_name"] = "Stepanov";
        newRow["age"] = 19;

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

        changeRow["age"] = 25;

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
        changeRow2["age"] = 25;

        DataRow deleteRow2 = table.Rows[1];
        deleteRow2.Delete();

        DataRow newRow2 = table.NewRow();
        newRow2["nickname"] = "krishleb";
        newRow2["first_name"] = "Roman";
        newRow2["last_name"] = "Stepanov";
        newRow2["age"] = 19;
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

    static void ChangeLog(DbProviderFactory factory)
    {
        DataTable table = LoadStudents(factory);

        int wasRows = table.Rows.Count;

        DataRow changeRow = table.Rows[0];
        changeRow["first_name"] = changeRow["first_name"] + " " + changeRow["last_name"];

        DataRow newRow = table.NewRow();
        newRow["nickname"] = "novikova";
        newRow["first_name"] = "Мария";
        newRow["last_name"] = "Иванова";
        newRow["age"] = 20;
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

    static void TwoTables(DbProviderFactory factory)
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

    static void FilterAndSort(DbProviderFactory factory)
    {
        DataTable table = LoadStudents(factory);

        DataView view = new DataView(table);

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1. Старше 18");
            Console.WriteLine("2. Младше 20");
            Console.WriteLine("3. Фамилия на С");
            Console.WriteLine("4. Без фильтра, сортировка по имени");
            Console.WriteLine("5. Свой фильтр");
            Console.WriteLine("0. Назад");
            Console.Write("Выберите фильтр: ");

            string choice = Console.ReadLine() ?? "";

            if (choice == "0")
            {
                break;
            }

            if (choice == "1")
            {
                view.RowFilter = "age > 18";
                view.Sort = "first_name";
            }
            else if (choice == "2")
            {
                view.RowFilter = "age < 20";
                view.Sort = "first_name";
            }
            else if (choice == "3")
            {
                view.RowFilter = "last_name LIKE 'С%'";
                view.Sort = "last_name";
            }
            else if (choice == "4")
            {
                view.RowFilter = "";
                view.Sort = "first_name";
            }
            else if (choice == "5")
            {
                Console.Write("Введите RowFilter: ");
                view.RowFilter = Console.ReadLine() ?? "";

                Console.Write("Введите Sort: ");
                view.Sort = Console.ReadLine() ?? "";
            }
            else
            {
                Console.WriteLine("Неверный пункт.");
                continue;
            }

            Console.WriteLine();
            Console.WriteLine("RowFilter: " + view.RowFilter);
            Console.WriteLine("Sort: " + view.Sort);
            Console.WriteLine("Показано строк: " + view.Count);

            foreach (DataRowView rowView in view)
            {
                Console.WriteLine("  " + rowView["id"] + " | " + rowView["first_name"]
                    + " " + rowView["last_name"] + " | возраст " + rowView["age"]);
            }

            Console.WriteLine();
            Console.WriteLine("RowFilter и Sort изменяют только этот просмотр, в PostgreSQL ничего не уходит.");
        }
    }

    static void SaveChanges(DbProviderFactory factory)
    {
        DataTable table = LoadStudents(factory);

        int wasRows = table.Rows.Count;

        DataRow? changeRow = null;
        DataRow? deleteRow = null;

        foreach (DataRow row in table.Rows)
        {
            if (row["nickname"].ToString() == "Krishleb")
            {
                if (changeRow == null)
                {
                    changeRow = row;
                }
                else if (deleteRow == null)
                {
                    deleteRow = row;
                }
            }
        }

        if (changeRow == null || deleteRow == null)
        {
            Console.WriteLine("Нужны две строки с nickname = Krishleb: одну меняем, другую удаляем.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Меняем строку ID " + changeRow["id"] + " (" + changeRow["last_name"] + ")");
        Console.WriteLine("Удаляем строку ID " + deleteRow["id"] + " (" + deleteRow["last_name"] + ")");

        changeRow["age"] = 25;

        DataRow newRow = table.NewRow();
        newRow["nickname"] = "krishleb";
        newRow["first_name"] = "Пётр";
        newRow["last_name"] = "Новиков";
        newRow["age"] = 20;
        table.Rows.Add(newRow);

        deleteRow.Delete();

        Console.WriteLine();
        Console.WriteLine("=== Состояния перед сохранением ===");
        PrintTable(table);

        Console.Write("Сохранить изменения в PostgreSQL? (y/n): ");

        if ((Console.ReadLine() ?? "") != "y")
        {
            Console.WriteLine("Сохранение отменено. Изменения остались только в памяти.");
            return;
        }

        using DbConnection connection = factory.CreateConnection()!;
        connection.ConnectionString = myConnString;
        connection.Open();

        using DbTransaction transaction = connection.BeginTransaction();

        using DbDataAdapter adapter = factory.CreateDataAdapter()!;

        adapter.InsertCommand = MakeInsert(factory, connection, transaction);
        adapter.UpdateCommand = MakeUpdate(factory, connection, transaction);
        adapter.DeleteCommand = MakeDelete(factory, connection, transaction);

        try
        {
            int rows = adapter.Update(table);

            transaction.Commit();

            Console.WriteLine();
            Console.WriteLine("Сохранено. Затронуто строк: " + rows);
        }
        catch (Exception ex)
        {
            transaction.Rollback();

            Console.WriteLine();
            Console.WriteLine("Не удалось сохранить: " + ex.Message);
            Console.WriteLine("Транзакция откачена — в PostgreSQL ничего не изменилось.");
            Console.WriteLine();
            Console.WriteLine("Изменения остались в памяти:");
            PrintTable(table);

            return;
        }

        Console.WriteLine();
        Console.WriteLine("=== Состояния после сохранения ===");
        PrintTable(table);

        Console.WriteLine();
        Console.WriteLine("=== Проверка отдельным запросом ===");

        using (DbCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT id, nickname, first_name, last_name, age FROM public.students_lesson14 ORDER BY id";

            using DbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine("  " + reader["id"] + " | " + reader["nickname"]
                    + " | " + reader["first_name"] + " " + reader["last_name"]
                    + " | возраст " + reader["age"]);
            }
        }

        Console.WriteLine();
        Console.WriteLine("Было строк: " + wasRows + ", стало: " + table.Rows.Count);
    }

    static DbCommand MakeInsert(DbProviderFactory factory, DbConnection connection, DbTransaction transaction)
    {
        DbCommand cmd = factory.CreateCommand()!;
        cmd.Connection = connection;
        cmd.Transaction = transaction;
        cmd.CommandText = "INSERT INTO public.students_lesson14 (nickname, first_name, last_name, age) "
            + "VALUES (@nickname, @first_name, @last_name, @age)";

        AddParameter(cmd, "@nickname", "nickname");
        AddParameter(cmd, "@first_name", "first_name");
        AddParameter(cmd, "@last_name", "last_name");
        AddParameter(cmd, "@age", "age");

        return cmd;
    }

    static DbCommand MakeUpdate(DbProviderFactory factory, DbConnection connection, DbTransaction transaction)
    {
        DbCommand cmd = factory.CreateCommand()!;
        cmd.Connection = connection;
        cmd.Transaction = transaction;
        cmd.CommandText = "UPDATE public.students_lesson14 "
            + "SET nickname = @nickname, first_name = @first_name, last_name = @last_name, age = @age "
            + "WHERE id = @id";

        AddParameter(cmd, "@nickname", "nickname");
        AddParameter(cmd, "@first_name", "first_name");
        AddParameter(cmd, "@last_name", "last_name");
        AddParameter(cmd, "@age", "age");

        DbParameter id = cmd.CreateParameter();
        id.ParameterName = "@id";
        id.SourceColumn = "id";
        id.SourceVersion = DataRowVersion.Original;
        cmd.Parameters.Add(id);

        return cmd;
    }

    static DbCommand MakeDelete(DbProviderFactory factory, DbConnection connection, DbTransaction transaction)
    {
        DbCommand cmd = factory.CreateCommand()!;
        cmd.Connection = connection;
        cmd.Transaction = transaction;
        cmd.CommandText = "DELETE FROM public.students_lesson14 WHERE id = @id";

        DbParameter id = cmd.CreateParameter();
        id.ParameterName = "@id";
        id.SourceColumn = "id";
        id.SourceVersion = DataRowVersion.Original;
        cmd.Parameters.Add(id);

        return cmd;
    }

    static void AddParameter(DbCommand cmd, string name, string column)
    {
        DbParameter parameter = cmd.CreateParameter();
        parameter.ParameterName = name;
        parameter.SourceColumn = column;
        cmd.Parameters.Add(parameter);
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
                    + ", Возраст: " + row["age", DataRowVersion.Original]
                    + ", Состояние: " + row.RowState);
            }
            else
            {
                Console.WriteLine("ID: " + row["id"]
                    + ", Имя: " + row["first_name"]
                    + ", Фамилия: " + row["last_name"]
                    + ", Возраст: " + row["age"]
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
