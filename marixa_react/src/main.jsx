import {applyTheme,initialTheme} from './services/theme/theme';
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './theme.css'
import './index.css'
import App from './App.jsx'
import './mobile.css'
import './components/common/workspace.css'
import './modules/me/me-workspace.css'
import './components/common/workspace-colors.css'
import { LanguageProvider } from "./services/i18n/LanguageProvider.jsx";

import './contrast-palette.css';
import './erp-theme.css';

applyTheme(initialTheme(localStorage, matchMedia('(prefers-color-scheme: dark)').matches));

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <LanguageProvider><App /></LanguageProvider>
  </StrictMode>,
)
