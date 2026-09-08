import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The FTP account only has write access under this path on the server,
// so the built site must live at this base URL.
const BASE_PATH = '/f8622112/'

// https://vite.dev/config/
export default defineConfig({
  base: BASE_PATH,
  plugins: [react()],
})
