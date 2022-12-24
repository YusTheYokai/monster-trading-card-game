using System.Reflection;
using System.Text;
using Npgsql;

namespace MonsterTradingCardGame.Data {

    internal abstract class Repository<T> where T : Entity {

        protected readonly EntityManager _entityManager = EntityManager.Instance;

        // /////////////////////////////////////////////////////////////////////
        // Methods
        // /////////////////////////////////////////////////////////////////////

        /// <summary>
        ///     Saves the given entity.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>The saved entity</returns>
        public T? Save(T entity) {
            if (entity.IsPersisted()) {
                return Update(entity);
            } else {
                return Insert(entity);
            }
        }

        /// <summary>
        ///     Find entity by its id.
        /// </summary>
        /// <param name="id">Id of the entity</param>
        /// <returns>The entity</returns>
        public T? FindById(Guid id) {
            string query = $"SELECT * FROM {typeof(T).Name} WHERE id = :id";
            var command = new NpgsqlCommand(query, _entityManager.connection) {
                Parameters = {
                    new(":id", id)
                }
            };
            var result = command.ExecuteReader();
            return ConstructEntity(result);
        }

        /// <summary>
        ///     Find all entities.
        /// </summary>
        /// <returns>List of entities</returns>
        public List<T> FindAll() {
            string query = $"SELECT * FROM {typeof(T).Name};";
            var result = new NpgsqlCommand(query, _entityManager.connection).ExecuteReader();
            return ConstructEntityList(result);
        }

        /// <summary>
        ///     Delete entity.
        /// </summary>
        /// <param name="id">Id of the entity</param>
        public void Delete(Guid id) {
            string query = $"DELETE FROM {typeof(T).Name} WHERE id = :id";
            var command = new NpgsqlCommand(query, _entityManager.connection) {
                Parameters = {
                    new(":id", id)
                }
            };
            command.ExecuteNonQuery();
        }

        // Helper
        // /////////////////////////////////////////////////////////////////////

        protected static T? ConstructEntity(NpgsqlDataReader result, bool close = true) {
            if (!result.Read()) {
                result.Close();
                return null;
            }

            object?[] values = new object[result.FieldCount];
            for (int i = 0; i < result.FieldCount; i++) {
                object value = result.GetValue(i);
                values[i] = value.GetType() == typeof(DBNull) ? null : value;
            }

            if (close) {
                result.Close();
            }

            return typeof(T).GetConstructors()[0].Invoke(values) as T;
        }

        protected static List<T> ConstructEntityList(NpgsqlDataReader result) {
            List<T> entities = new();
            T? entity;

            while ((entity = ConstructEntity(result, false)) != null) {
                entities.Add(entity);
            }

            result.Close();
            return entities;
        }

        // Save
        // /////////////////////////////////////////////////////////////////////

        private T? Insert(T entity) {
            PropertyInfo[] properties = typeof(T).GetProperties();
            string query = $"INSERT INTO {typeof(T).Name} ({PropertiesToString(properties)}) VALUES ({ValuesOfPropertiesToString(properties, entity)}) RETURNING ID";
            Guid id = (Guid) new NpgsqlCommand(query, _entityManager.connection).ExecuteScalar();
            return FindById(id);
        }

        private static string PropertiesToString(PropertyInfo[] properties) {
            StringBuilder sb = new();
            foreach (PropertyInfo property in properties) {
                if (property.Name != "id") {
                    Column? column = property.GetCustomAttribute<Column>();
                    sb.Append(column == null ? property.Name : column.Name);
                    sb.Append(',');
                }
            }
            return sb.ToString()[..^1];
        }

        private static string ValuesOfPropertiesToString(PropertyInfo[] properties, T entity) {
            StringBuilder sb = new();
            foreach (PropertyInfo property in properties) {
                if (property.Name != "id") {
                    object? value = property.GetValue(entity);
                    if (value == null) {
                        sb.Append("null");
                    } else if (property.PropertyType == typeof(string) || property.PropertyType == typeof(Guid?) || property.PropertyType == typeof(Guid)) {
                        sb.Append('\'');
                        sb.Append(value);
                        sb.Append('\'');
                    } else if (property.PropertyType.IsEnum) {
                        sb.Append((int) value);
                    } else {
                        sb.Append(property.GetValue(entity));
                    }
                    sb.Append(',');
                }
            }
            return sb.ToString()[..^1];
        }

        private T? Update(T entity) {
            PropertyInfo[] properties = typeof(T).GetProperties();

            string query = $"UPDATE {typeof(T).Name} SET {FieldAndValuePairsToString(properties, entity)} WHERE id = :id RETURNING id;";
            Guid id = (Guid) new NpgsqlCommand(query, _entityManager.connection) {
                Parameters = { new(":id", entity.id) }
            }.ExecuteScalar();

            return FindById(id);
        }

        private static string FieldAndValuePairsToString(PropertyInfo[] properties, T entity) {
            StringBuilder sb = new();
            foreach (PropertyInfo property in properties) {
                if (property.Name != "id") {
                    Column? column = property.GetCustomAttribute<Column>();
                    sb.Append(column == null ? property.Name : column.Name);
                    sb.Append(" = ");

                    object? value = property.GetValue(entity);
                    if (value == null) {
                        sb.Append("null");
                    } else if (property.PropertyType == typeof(string) || property.PropertyType == typeof(Guid?)) {
                        sb.Append('\'');
                        sb.Append(property.GetValue(entity));
                        sb.Append('\'');
                    } else if (property.PropertyType.IsEnum) {
                        object? enumValue = property.GetValue(entity);
                        if (enumValue != null) {
                            sb.Append((int) enumValue);
                        }
                    } else {
                        sb.Append(property.GetValue(entity));
                    }

                    sb.Append(", ");
                }
            }
            return sb.ToString()[..^2];
        }
    }
}
