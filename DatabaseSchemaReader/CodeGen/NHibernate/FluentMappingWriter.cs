﻿using System;
using System.Globalization;
using System.Linq;
using System.Text;
using DatabaseSchemaReader.DataSchema;

namespace DatabaseSchemaReader.CodeGen.NHibernate
{
    class FluentMappingWriter
    {
        private readonly DatabaseTable _table;
        private readonly CodeWriterSettings _codeWriterSettings;
        private readonly MappingNamer _mappingNamer;
        private readonly ClassBuilder _cb;

        public FluentMappingWriter(DatabaseTable table, CodeWriterSettings codeWriterSettings, MappingNamer mappingNamer)
        {
            if (table == null) throw new ArgumentNullException("table");
            if (mappingNamer == null) throw new ArgumentNullException("mappingNamer");

            _codeWriterSettings = codeWriterSettings;
            _mappingNamer = mappingNamer;
            _table = table;
            _cb = new ClassBuilder();
        }

        /// <summary>
        /// Gets the name of the mapping class.
        /// </summary>
        /// <value>
        /// The name of the mapping class.
        /// </value>
        public string MappingClassName { get; private set; }

        public string Write()
        {
            _cb.AppendLine("using FluentNHibernate.Mapping;");

            MappingClassName = _mappingNamer.NameMappingClass(_table.NetName);

            using (_cb.BeginNest("namespace " + _codeWriterSettings.Namespace + ".Mapping"))
            {
                using (_cb.BeginNest("public class " + MappingClassName + " : ClassMap<" + _table.NetName + ">", "Class mapping to " + _table.Name + " table"))
                {
                    using (_cb.BeginNest("public " + MappingClassName + "()", "Constructor"))
                    {
                        if (_table.Name != _table.NetName)
                        {
                            var name = _table.Name;
                            if (name.Contains(" ")) name = "`" + name + "`";
                            _cb.AppendFormat("Table(\"{0}\");", name);
                        }

                        AddPrimaryKey();
                        WriteColumns();

                        foreach (var foreignKeyChild in _table.ForeignKeyChildren)
                        {
                            WriteForeignKeyCollection(foreignKeyChild);
                        }
                    }
                }
            }

            return _cb.ToString();
        }

        private void AddPrimaryKey()
        {
            if (_table.PrimaryKey == null || _table.PrimaryKey.Columns.Count == 0)
            {
                if (_table is DatabaseView)
                {
                    AddCompositePrimaryKeyForView();
                    return;
                }
                _cb.AppendLine("//TODO- you MUST add a primary key!");
                return;
            }
            if (_table.HasCompositeKey)
            {
                AddCompositePrimaryKey();
                return;
            }

            var idColumn = _table.PrimaryKeyColumn;

            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "Id(x => x.{0})", idColumn.NetName);
            if (idColumn.Name != idColumn.NetName)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ".Column(\"{0}\")", idColumn.Name);
            }
            if (idColumn.IsIdentity)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ".GeneratedBy.Identity()");
                //other GeneratedBy values (Guid, Assigned) are left to defaults
            }

            sb.Append(";");
            _cb.AppendLine(sb.ToString());
        }

        private void AddCompositePrimaryKeyForView()
        {
            var sb = new StringBuilder();
            sb.Append("CompositeId()");
            //we map ALL columns as the key.
            foreach (var column in _table.Columns)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture,
                                ".KeyProperty(x => x.{0}, \"{1}\")",
                                column.NetName,
                                column.Name);
            }
            sb.Append(";");
            _cb.AppendLine(sb.ToString());
            _cb.AppendLine("ReadOnly();");
        }

        private void AddCompositePrimaryKey()
        {

            var sb = new StringBuilder();
            //our convention is always to generate a key class with property name Key
            sb.Append("CompositeId(x => x.Key)");
						
						// KL: Ensuring composite primary key is generated in order of define key and not
						// simply the order of the tables.
						foreach (var col in _table.PrimaryKey.Columns)
						{
							DatabaseColumn column = _table.FindColumn(col);

							// KL: Mapping the foreign keys separately. KeyReference was causing issues.
							var keyType = "KeyReference";
							//if (column.ForeignKeyTable == null)
							{
								keyType = "KeyProperty";
							}
							sb.AppendFormat(CultureInfo.InvariantCulture,
															".{0}(x => x.{1}, \"{2}\")",
															keyType,
															column.NetName,
															column.Name);
						}
            sb.Append(";");
            _cb.AppendLine(sb.ToString());
        }

        private void WriteColumns()
        {
            //map the columns
						// KL: Only write empty columns. Then, foreign keys.
            foreach (var column in _table.Columns.Where(c => !c.IsPrimaryKey && !c.IsForeignKey))
            {
                WriteColumn(column);
            }

						// KL: Writing foreign key separately
						foreach (var fKey in _table.ForeignKeys)
						{
							WriteForeignKey(fKey);
						}
        }

				

        private void WriteColumn(DatabaseColumn column)
        {
            if (column.IsForeignKey)
            {
							// KL: Needed to write foreign keys in their own step in order to support composite
                //WriteForeignKey(column);
                return;
            }

            var propertyName = column.NetName;
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "Map(x => x.{0})", propertyName);
            if (propertyName != column.Name)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ".Column(\"{0}\")", column.Name);
            }

            var dt = column.DataType;
            if (dt != null)
            {
                //nvarchar(max) may be -1
                if (dt.IsString && column.Length > 0 && column.Length < 1073741823)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ".Length({0})", column.Length.GetValueOrDefault());
                }
            }

            if (!column.Nullable)
            {
                sb.Append(".Not.Nullable()");
            }

            sb.Append(";");
            _cb.AppendLine(sb.ToString());
        }

        private void WriteForeignKey(DatabaseColumn column)
        {
            var propertyName = column.NetName;
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "References(x => x.{0})", propertyName);
            sb.AppendFormat(CultureInfo.InvariantCulture, ".Column(\"{0}\")", column.Name);
            //bad idea unless you expect the database to be inconsistent
            //sb.Append(".NotFound.Ignore()");
            //could look up cascade rule here
            sb.Append(";");
            _cb.AppendLine(sb.ToString());
        }

				/// <summary>
				/// KL: Writes the foreign key an also supports composite foreign keys.
				/// </summary>
				/// <param name="fKey">The f key.</param>
				private void WriteForeignKey(DatabaseConstraint fKey)
				{
					var propertyName = "Unknown";
					var refTable = _table.DatabaseSchema.FindTableByName(fKey.RefersToTable);
					var parentTable = _table.DatabaseSchema.FindTableByName(fKey.TableName);
					if (refTable != null)
						propertyName = refTable.NetName;

					// Check whether the referenced table is used in any other key. This ensures that the property names
					// are unique.
					if (parentTable.ForeignKeys.Count(x => x.RefersToTable == fKey.RefersToTable) > 1)
					{
						// Append the key name to the property name. In the event of multiple foreign keys to the same table
						// This will give the consumer context.
						propertyName += fKey.Name;
					}

					// Ensures that property name cannot be the same as class name
					if (propertyName == parentTable.NetName)
					{
						propertyName += "Key";
					}

					var sb = new StringBuilder();

					var cols = fKey.Columns.Select(x => string.Format("\"{0}\"", x)).ToArray();
					
					sb.AppendFormat(CultureInfo.InvariantCulture, "References(x => x.{0})", propertyName);
					if (cols.Length > 1)
					{
						sb.AppendFormat(CultureInfo.InvariantCulture, ".Columns(new string[] {{ {0} }});", String.Join(", ", cols));
					}
					else
					{
						sb.AppendFormat(CultureInfo.InvariantCulture, ".Column(\"{0}\");", fKey.Columns.FirstOrDefault());
					}
					_cb.AppendLine(sb.ToString());
				}

        private void WriteForeignKeyCollection(DatabaseTable foreignKeyChild)
        {
            var foreignKeyTable = foreignKeyChild.Name;
            var childClass = foreignKeyChild.NetName;
            var foreignKey = foreignKeyChild.ForeignKeys.FirstOrDefault(fk => fk.RefersToTable == _table.Name);
            if (foreignKey == null) return; //corruption in our database
            //we won't deal with composite keys
						var fkColumn = foreignKey.Columns[0];

            _cb.AppendFormat("//Foreign key to {0} ({1})", foreignKeyTable, childClass);
            var propertyName = _codeWriterSettings.NameCollection(childClass);

            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "HasMany(x => x.{0})", propertyName);
            //defaults to x_id
					
						// KL: Only use .KeyColumn() if the foreign key is not composite
						if (foreignKey.Columns.Count == 1)
						{
							sb.AppendFormat(CultureInfo.InvariantCulture, ".KeyColumn(\"{0}\")", fkColumn);
						}
						// If composite key, generate .KeyColumns(...) with array of keys
						else
						{
							var cols = foreignKey.Columns.Select(x => string.Format("\"{0}\"", x)).ToArray();
							sb.AppendFormat(CultureInfo.InvariantCulture, ".KeyColumns.Add(new string[] {{ {0} }})", String.Join(", ", cols));
						}
            sb.Append(".Inverse()");
            sb.AppendFormat(CultureInfo.InvariantCulture, ".ForeignKeyConstraintName(\"{0}\")", foreignKey.Name);

            sb.Append(";");
            _cb.AppendLine(sb.ToString());
        }
    }
}
