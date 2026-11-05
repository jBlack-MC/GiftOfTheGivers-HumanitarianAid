package com.example.giftofthegivers_mobile.ui.components

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import com.example.giftofthegivers_mobile.viewmodel.Screen

@Composable
fun BottomNavBar(
    currentScreen: Screen,
    onNavigate: (Screen) -> Unit
) {
    NavigationBar {
        NavigationBarItem(
            selected = currentScreen is Screen.Home,
            onClick = { onNavigate(Screen.Home) },
            icon = { Icon(Icons.Default.Home, contentDescription = "Home") },
            label = { Text("Home") }
        )
        NavigationBarItem(
            selected = currentScreen is Screen.Projects,
            onClick = { onNavigate(Screen.Projects) },
            icon = { Icon(Icons.Default.VolunteerActivism, contentDescription = "Projects") },
            label = { Text("Projects") }
        )
        NavigationBarItem(
            selected = currentScreen is Screen.Donate,
            onClick = { onNavigate(Screen.Donate) },
            icon = { Icon(Icons.Default.Favorite, contentDescription = "Donate") },
            label = { Text("Donate") }
        )
        NavigationBarItem(
            selected = currentScreen is Screen.Volunteer,
            onClick = { onNavigate(Screen.Volunteer) },
            icon = { Icon(Icons.Default.Group, contentDescription = "Volunteer") },
            label = { Text("Volunteer") }
        )
        NavigationBarItem(
            selected = currentScreen is Screen.DonorPortal,
            onClick = { onNavigate(Screen.DonorPortal) },
            icon = { Icon(Icons.Default.Person, contentDescription = "Portal") },
            label = { Text("Portal") }
        )
    }
}
