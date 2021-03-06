﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable UnusedParameter.Local

namespace Norma.Delta.SQLite
{
    /// <sumary>
    ///     Converts provider-agnostic migration operations into database provider specific SQL commands for SQLite.
    /// </sumary>
    /// <remarks>
    ///     In the constructor of your migrations Configuration class, add:
    ///     SetSqlGenerator( "System.Data.SQLite", new SQLite.CodeFirst.SqliteMigrationSqlGenerator() );
    /// </remarks>
    public sealed class SqliteMigrationSqlGenerator : MigrationSqlGenerator
    {
        #region Overrides from MigrationSqlGenerator

        /// <summary>
        ///     Converts a set of migration operations into database provider specific SQL.
        /// </summary>
        /// <param name="migrationOperations">The operations to be converted.</param>
        /// <param name="providerManifestToken">Token representing the version of the database being targeted.</param>
        /// <returns>A list of SQL statements to be executed to perform the migration operations.</returns>
        public override IEnumerable<MigrationStatement> Generate(
            IEnumerable<MigrationOperation> migrationOperations, string providerManifestToken)
        {
            var impl = new Impl(providerManifestToken);
            impl.Generate(migrationOperations);
            return impl.MigrationStatements;
        }

        #endregion

        private sealed class Impl
        {
            #region Constantes

            private const string ProviderInvariantName = "System.Data.SQLite";
            private const string DefaultDateTimeFormat = "yyyy-MM-dd hh:mm:ss";
            private const int DefaultStringMaxLength = 255;
            private const int DefaultNumericPrecision = 10;
            private const byte DefaultTimePrecision = 7;
            private const byte DefaultNumericScale = 0;

            #endregion

            #region Instancias

            private readonly DbProviderServices _providerServices;
            private readonly DbProviderManifest _providerManifest;
            private readonly List<MigrationStatement> _migrationStatements;

            #endregion

            #region Public Surface

            public Impl(string providerManifestToken)
            {
                _migrationStatements = new List<MigrationStatement>();

                var factory = DbProviderFactories.GetFactory(ProviderInvariantName);

                using (var connection = factory.CreateConnection())
                {
                    _providerServices = DbProviderServices.GetProviderServices(connection);
                    _providerManifest = _providerServices.GetProviderManifest(providerManifestToken);
                }
            }

            public IEnumerable<MigrationStatement> MigrationStatements
            {
                get { return _migrationStatements; }
            }

            public void Generate(IEnumerable<MigrationOperation> migrationOperations)
            {
                foreach (dynamic dynamicOperation in migrationOperations)
                    Generate(dynamicOperation);
            }

            #endregion

            #region Migration Statement Generation

            private void Generate(CreateTableOperation op)
            {
                using (var tw = CreateIndentedTextWriter())
                {
                    GenerateCreateTableCommand(op, tw);

                    AddSqlStatement(tw);
                }
            }

            private void Generate(AddForeignKeyOperation op)
            {
                // Currently not supported.
                throw new NotSupportedException();
            }

            private void Generate(DropForeignKeyOperation op)
            {
                // Currently not supported.
                throw new NotSupportedException();
            }

            private void Generate(CreateIndexOperation op)
            {
                using (var tw = CreateIndentedTextWriter())
                {
                    tw.Write("CREATE ");

                    if (op.IsUnique)
                        tw.Write(" UNIQUE ");

                    tw.Write("INDEX ");
                    tw.Write(op.Name);
                    tw.Write(" ON ");
                    tw.Write(RemoveDbo(op.Table));
                    tw.Write("(");

                    for (var i = 0; i < op.Columns.Count; i++)
                    {
                        tw.Write(op.Columns[i]);

                        if (i < op.Columns.Count - 1)
                            tw.WriteLine(",");
                    }

                    tw.Write(")");

                    AddSqlStatement(tw);
                }
            }

            private void Generate(DropIndexOperation op)
            {
                using (var tw = CreateIndentedTextWriter())
                {
                    tw.Write("DROP INDEX ");
                    tw.Write(RemoveDbo(op.Name));

                    AddSqlStatement(tw);
                }
            }

            private void Generate(AddPrimaryKeyOperation op)
            {
                throw new NotImplementedException(
                    "AddPrimaryKey is non-trivial and has not been implemented. See http://sqlite.org/lang_altertable.html");
            }

            private void Generate(DropPrimaryKeyOperation op)
            {
                throw new NotImplementedException(
                    "DropPrimaryKey is non-trivial and has not been implemented. See http://sqlite.org/lang_altertable.html");
            }

            private void Generate(AddColumnOperation op)
            {
                using (var tw = CreateIndentedTextWriter())
                {
                    tw.Write("ALTER TABLE ");
                    tw.Write(RemoveDbo(op.Table));
                    tw.Write(" ADD ");

                    var col = op.Column;

                    Generate(col, tw, null);

                    if ((col.IsNullable != null)
                        && !col.IsNullable.Value
                        && (col.DefaultValue == null)
                        && string.IsNullOrWhiteSpace(col.DefaultValueSql)
                        && !col.IsIdentity
                        && !col.IsTimestamp
                        && (
                            col.StoreType == null || (
                                !col.StoreType.Equals("rowversion", StringComparison.InvariantCultureIgnoreCase)
                                && !col.StoreType.Equals("timestamp", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        tw.Write(" DEFAULT ");

                        if (col.Type == PrimitiveTypeKind.DateTime)
                        {
                            tw.Write(Generate(DateTime.Parse("1900-01-01 00:00:00", CultureInfo.InvariantCulture)));
                        }
                        else
                        {
                            tw.Write(Generate((dynamic) col.ClrDefaultValue));
                        }
                    }

                    AddSqlStatement(tw);
                }
            }

            private void Generate(DropColumnOperation op)
            {
                throw new NotImplementedException(
                    "DropColumn is non-trivial and has not been implemented. See http://sqlite.org/lang_altertable.html");
            }

            private void Generate(AlterColumnOperation op)
            {
                throw new NotImplementedException(
                    "AlterColumn is non-trivial and has not been implemented. See http://sqlite.org/lang_altertable.html");
            }

            private void Generate(DropTableOperation op)
            {
                using (var tw = CreateIndentedTextWriter())
                {
                    tw.Write("DROP TABLE ");
                    tw.Write(RemoveDbo(op.Name));

                    AddSqlStatement(tw);
                }
            }

            private void Generate(SqlOperation op)
            {
                AddSqlStatement(op.Sql, op.SuppressTransaction);
            }

            private void Generate(RenameColumnOperation op)
            {
                throw new NotImplementedException(
                    "RenameColumn is non-trivial and has not been implemented. See http://sqlite.org/lang_altertable.html");
            }

            private void Generate(RenameTableOperation op)
            {
                using (var tw = CreateIndentedTextWriter())
                {
                    tw.Write("ALTER TABLE ");
                    tw.Write(RemoveDbo(op.Name));
                    tw.Write(" RENAME TO ");
                    tw.Write(RemoveDbo(op.NewName));

                    AddSqlStatement(tw);
                }
            }

            private void Generate(MoveTableOperation op)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            ///     Generate column definition. Returns <c>true</c> if the column was the primary key.
            /// </summary>
            private bool Generate(ColumnModel column, IndentedTextWriter tw, PrimaryKeyOperation primaryKeyOp)
            {
                var isIdPk = false;

                tw.Write(column.Name);
                tw.Write(" ");
                var isPrimaryKey = false;

                if (primaryKeyOp != null)
                    isPrimaryKey = primaryKeyOp.Columns.Contains(column.Name);

                if (isPrimaryKey)
                {
                    if ((column.Type == PrimitiveTypeKind.Int16) ||
                        (column.Type == PrimitiveTypeKind.Int32))
                        tw.Write(" INTEGER ");
                    else
                        tw.Write(BuildColumnType(column));

                    if (column.IsIdentity)
                    {
                        tw.Write(" PRIMARY KEY AUTOINCREMENT ");
                        isIdPk = true;
                    }
                }
                else
                {
                    tw.Write(BuildColumnType(column));

                    if ((column.IsNullable != null)
                        && !column.IsNullable.Value)
                    {
                        tw.Write(" NOT NULL");
                    }

                    if (column.DefaultValue != null)
                    {
                        tw.Write(" DEFAULT ");
                        tw.Write(Generate((dynamic) column.DefaultValue));
                    }
                    else if (!string.IsNullOrWhiteSpace(column.DefaultValueSql))
                    {
                        tw.Write(" DEFAULT ");
                        tw.Write(column.DefaultValueSql);
                    }
                }

                return isIdPk;
            }

            private static readonly Regex RxMatchParameterReference = new Regex("@p[0-9]+",
                                                                                RegexOptions.Compiled |
                                                                                RegexOptions.CultureInvariant |
                                                                                RegexOptions.Multiline);

            private void Generate(HistoryOperation operation)
            {
                foreach (var cmdTree in operation.CommandTrees)
                {
                    // Note: For comparison, within the SqlServer-specific implementation, there's a method equivalent to
                    // CreateCommandDefinition that takes a boolean flag "generateParameters". This controls whether paremeters
                    // are generated as references (ie, "@p0" etc) or literals (ie, "'SomeLiteralString'" etc). There's no
                    // equivalent exposed from System.Data.SQLite. So instead we replace parameter references with the literals
                    // within the generated CommandText. The former approach would, of course, be preferable if it were
                    // available.

                    var cmdDef = _providerServices.CreateCommandDefinition(_providerManifest, cmdTree);
                    var cmd = cmdDef.CreateCommand();
                    var cmdText = cmd.CommandText;

                    if (cmd.Parameters.Count > 0)
                    {
                        cmdText = RxMatchParameterReference.Replace(
                                                                    cmdText,
                                                                    m =>
                                                                    {
                                                                        var idx = int.Parse(m.Value.Substring(2));
                                                                        dynamic dynValue = cmd.Parameters[idx].Value;
                                                                        string literal = Generate(dynValue);
                                                                        return literal;
                                                                    });
                    }

                    AddSqlStatement(cmdText);
                }
            }

            /// <summary>
            ///     Generate byte array literal.
            /// </summary>
            private string Generate(byte[] v)
            {
                var sb = new StringBuilder(v.Length * 2 + 3);

                sb.Append("x'");

                foreach (var b in v)
                    sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));

                sb.Append("'");

                return sb.ToString();
            }

            /// <summary>
            ///     Generate boolean literal.
            /// </summary>
            private string Generate(bool v)
            {
                return v ? "1" : "0";
            }

            /// <summary>
            ///     Generate DateTime literal.
            /// </summary>
            private string Generate(DateTime v)
            {
                return "'" + v.ToString(DefaultDateTimeFormat, CultureInfo.InvariantCulture) + "'";
            }

            /// <summary>
            ///     Generate DateTimeOffSet literal.
            /// </summary>
            private string Generate(DateTimeOffset v)
            {
                return "'" + v.ToString(DefaultDateTimeFormat, CultureInfo.InvariantCulture) + "'";
            }

            /// <summary>
            ///     Generate Guid literal.
            /// </summary>
            private string Generate(Guid v)
            {
                return "'" + v + "'";
            }

            /// <summary>
            ///     Generate string literal.
            /// </summary>
            private string Generate(string v)
            {
                return "'" + v.Replace("'", "''") + "'";
            }

            /// <summary>
            ///     Generate TimeSpan literal.
            /// </summary>
            private string Generate(TimeSpan v)
            {
                return "'" + v + "'";
            }

            /// <summary>
            ///     Generate literal for other object.
            /// </summary>
            private string Generate(object v)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", v);
            }

            #endregion

            #region Support methods.

            /// <summary>
            ///     Builds a column type
            /// </summary>
            /// <returns> SQL representing the data type. </returns>
            private string BuildColumnType(ColumnModel column)
            {
                return column.IsTimestamp ? "rowversion" : BuildPropertyType(column);
            }

            /// <summary>
            ///     Builds a SQL property type fragment from the specified <see cref="ColumnModel" />.
            /// </summary>
            /// <param name="column"></param>
            /// <returns></returns>
            private string BuildPropertyType(ColumnModel column)
            {
                var originalStoreType = column.StoreType;

                if (string.IsNullOrWhiteSpace(originalStoreType))
                {
                    var typeUsage = _providerManifest.GetStoreType(column.TypeUsage).EdmType;
                    originalStoreType = typeUsage.Name;
                }

                var storeType = originalStoreType;

                const string maxSuffix = "(max)";

                if (storeType.EndsWith(maxSuffix, StringComparison.Ordinal))
                    storeType = storeType.Substring(0, storeType.Length - maxSuffix.Length) + maxSuffix;

                switch (originalStoreType.ToLowerInvariant())
                {
                    case "decimal":
                    case "numeric":
                        storeType += "(" + (column.Precision ?? DefaultNumericPrecision)
                                     + ", " + (column.Scale ?? DefaultNumericScale) + ")";
                        break;

                    case "datetime":
                    case "time":
                        storeType += "(" + (column.Precision ?? DefaultTimePrecision) + ")";
                        break;

                    case "blob":
                    case "varchar2":
                    case "varchar":
                    case "char":
                    case "nvarchar":
                    case "nvarchar2":
                        storeType += "(" + (column.MaxLength ?? DefaultStringMaxLength) + ")";
                        break;
                }

                return storeType;
            }

            /// <summary>
            ///     Adds a new Statement to be executed against the database.
            /// </summary>
            /// <param name="sql"></param>
            /// <param name="suppressTransaction"></param>
            private void AddSqlStatement(string sql, bool suppressTransaction = false)
            {
                _migrationStatements.Add(new MigrationStatement
                {
                    Sql = sql,
                    SuppressTransaction = suppressTransaction
                });
            }

            /// <summary>
            ///     Adds a new Statement to be executed against the database.
            /// </summary>
            /// <param name="tw"> The writer containing the SQL to be executed. </param>
            private void AddSqlStatement(IndentedTextWriter tw)
            {
                AddSqlStatement(tw.InnerWriter.ToString());
            }

            /// <summary>
            ///     Gera um objeto <see cref="IndentedTextWriter" /> Utilizado para gerar os comandos SQL.
            /// </summary>
            /// <returns> An empty text writer to use for SQL generation. </returns>
            private IndentedTextWriter CreateIndentedTextWriter()
            {
                return new IndentedTextWriter(new StringWriter(CultureInfo.InvariantCulture));
            }

            /// <summary>
            ///     Remove occurences of "dbo." from the supplied string.
            /// </summary>
            private static string RemoveDbo(string str)
            {
                return str.Replace("dbo.", string.Empty);
            }

            /// <summary>
            ///     Generate CREATE TABLE command
            /// </summary>
            private void GenerateCreateTableCommand(CreateTableOperation op, IndentedTextWriter tw)
            {
                tw.WriteLine("CREATE TABLE " + RemoveDbo(op.Name) + " (");
                tw.Indent++;

                var hasGenereatedIdPk = false;

                for (var i = 0; i < op.Columns.Count; i++)
                {
                    var lcmDadosColuna = op.Columns.ToList()[i];
                    hasGenereatedIdPk |= Generate(lcmDadosColuna, tw, op.PrimaryKey);

                    if (i < op.Columns.Count - 1)
                        tw.WriteLine(",");
                }

                if ((op.PrimaryKey != null) && !hasGenereatedIdPk)
                {
                    tw.WriteLine(",");
                    tw.Write("CONSTRAINT ");
                    tw.Write(RemoveDbo(op.PrimaryKey.Name));
                    tw.Write(" PRIMARY KEY ");
                    tw.Write("(");

                    for (var li = 0; li < op.PrimaryKey.Columns.Count; li++)
                    {
                        var lstrNomeColuna = op.PrimaryKey.Columns[li];

                        tw.Write(lstrNomeColuna);

                        if (li < op.PrimaryKey.Columns.Count - 1)
                            tw.WriteLine(",");
                    }

                    tw.WriteLine(")");
                }
                else
                {
                    tw.WriteLine();
                }

                tw.Indent--;
                tw.Write(")");
            }

            #endregion
        }
    }
}