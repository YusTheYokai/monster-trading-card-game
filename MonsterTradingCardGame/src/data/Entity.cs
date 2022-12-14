namespace MonsterTradingCardGame.Data {

    public abstract class Entity {

        [Id]
        public Guid Id { get; }

        // /////////////////////////////////////////////////////////////////////
        // Constructors
        // /////////////////////////////////////////////////////////////////////

        public Entity() {
            // default constructor
        }

        /// <summary>
        ///     Constructor for constructing an entity by a <see cref="Repository{E}"/>.
        /// </summary>
        public Entity(Guid id) {
            Id = id;
        }

        // /////////////////////////////////////////////////////////////////////
        // Methods
        // /////////////////////////////////////////////////////////////////////

        public bool IsPersisted() {
            return Id != Guid.Empty;
        }
    }
}
