function CategoryFilter({ categories, selectedCategory, onCategoryChange, disabled }) {
  const categoryOptions = Array.from(
    new Set(
      categories
        .map((category) => String(category).trim())
        .filter((category) => category.length > 0),
    ),
  ).sort((first, second) => first.localeCompare(second, 'es'))

  return (
    <div className="category-filter">
      <label className="category-filter__label" htmlFor="category-filter">
        Categoría
      </label>
      <select
        className="category-filter__select"
        id="category-filter"
        value={selectedCategory}
        disabled={disabled}
        onChange={(event) => onCategoryChange(event.target.value)}
      >
        <option value="">Todas las categorías</option>
        {categoryOptions.map((category) => (
          <option key={category} value={category}>
            {category}
          </option>
        ))}
      </select>
    </div>
  )
}

export default CategoryFilter
