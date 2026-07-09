import './App.css'

function App() {
  return (
    <main className="app">
      <header className="app__header">
        <span className="app__logo" aria-hidden="true">◆</span>
        <h1 className="app__title">Hominder</h1>
      </header>

      <p className="app__tagline">
        Le socle du frontend est en place. Prêt à construire.
      </p>

      <section className="app__stack" aria-label="Stack technique">
        <span className="chip">React 19</span>
        <span className="chip">TypeScript</span>
        <span className="chip">Vite</span>
      </section>

      <footer className="app__footer">
        <code>src/App.tsx</code> — édite ce fichier pour démarrer.
      </footer>
    </main>
  )
}

export default App
