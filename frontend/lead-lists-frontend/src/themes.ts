import { createTheme } from "@mui/material/styles";

export const theme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: "#0b63d6",
      contrastText: "#ffffff",
    },
    secondary: {
      main: "#ff7043",
      contrastText: "#ffffff",
    },
    background: {
      default: "#ffffffff",
      paper: "#ffffff",
    },
    text: {
      primary: "#0f1724",
      secondary: "rgba(15,23,36,0.7)",
    },
  },
  typography: {
    fontFamily:
      '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
    h4: { fontWeight: 700 },
    body1: { fontSize: 20 },
  },
  shape: { borderRadius: 12 },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          WebkitFontSmoothing: "antialiased",
          MozOsxFontSmoothing: "grayscale",
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: "none",
          borderRadius: 12,
          border: "1px solid rgba(15,23,36,0.04)",
          boxShadow: "0 6px 20px rgba(15,23,36,0.06)",
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: "none",
          fontWeight: 700,
          borderRadius: 10,
          padding: "10px 16px",
        },
        containedPrimary: {
          backgroundColor: "#0b63d6",
          color: "#fff",
          boxShadow: "0 6px 14px rgba(11,99,214,0.12)",
          '&:hover': {
            backgroundColor: '#094fb3',
          },
        },
        containedSecondary: {
          backgroundColor: '#ff7043',
          color: '#fff',
          '&:hover': {
            backgroundColor: '#f4511e',
          },
        },
      },
    },
    MuiIconButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          padding: 6,
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        head: {
          backgroundColor: '#f0f3f5ff',
          color: '#0f1724',
          fontWeight: 800,
          fontSize: '1rem',
        },
      },
    },
    MuiDialog: {
      styleOverrides: {
        paper: {
          borderRadius: 12,
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          borderRadius: 10,
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          fontWeight: 600,
        },
      },
    },
  },
});