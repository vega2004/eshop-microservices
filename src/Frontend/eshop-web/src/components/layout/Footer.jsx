function Footer() {
  const currentYear = new Date().getFullYear()

  return (
    <footer className="site-footer">
      <div className="site-footer__content">
        <p className="site-footer__brand">E-Shop - {currentYear}</p>
        <p>Proyecto académico de comercio electrónico.</p>
        <p className="site-footer__tech">React · .NET · PostgreSQL · Redis</p>
      </div>
    </footer>
  )
}

export default Footer
