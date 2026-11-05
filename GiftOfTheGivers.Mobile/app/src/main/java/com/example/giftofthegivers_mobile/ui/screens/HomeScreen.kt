package com.example.giftofthegivers_mobile.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AdminPanelSettings
import androidx.compose.material.icons.filled.Security
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.giftofthegivers_mobile.data.ImpactStats
import com.example.giftofthegivers_mobile.data.ReliefProject
import com.example.giftofthegivers_mobile.data.UserRole
import com.example.giftofthegivers_mobile.ui.components.ImpactStatsView
import com.example.giftofthegivers_mobile.ui.components.ProjectCard
import com.example.giftofthegivers_mobile.viewmodel.Screen

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(
    impactStats: ImpactStats,
    projects: List<ReliefProject>,
    userRole: UserRole,
    onNavigate: (Screen) -> Unit,
    onDonateToProject: (String) -> Unit,
    onSwitchRole: (UserRole) -> Unit
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Gift of the Givers", fontWeight = FontWeight.Bold) },
                actions = {
                    IconButton(onClick = { onNavigate(Screen.Privacy) }) {
                        Icon(Icons.Default.Security, contentDescription = "Privacy Policy")
                    }
                    IconButton(onClick = {
                        if (userRole == UserRole.Employee) {
                            onSwitchRole(UserRole.Public)
                        } else {
                            onSwitchRole(UserRole.Employee)
                            onNavigate(Screen.StaffPortal)
                        }
                    }) {
                        Icon(
                            imageVector = Icons.Default.AdminPanelSettings,
                            contentDescription = "Staff Portal",
                            tint = if (userRole == UserRole.Employee) MaterialTheme.colorScheme.primary else LocalContentColor.current
                        )
                    }
                }
            )
        }
    ) { padding ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(horizontal = 16.dp)
        ) {
            item {
                Spacer(modifier = Modifier.height(8.dp))
                ImpactStatsView(stats = impactStats)
                Spacer(modifier = Modifier.height(16.dp))
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "Active Relief Projects",
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold
                    )
                    TextButton(onClick = { onNavigate(Screen.Projects) }) {
                        Text("View All")
                    }
                }
                Spacer(modifier = Modifier.height(8.dp))
            }

            items(projects.take(3)) { project ->
                ProjectCard(
                    project = project,
                    onDonateClick = { onDonateToProject(project.id) }
                )
            }

            item {
                Spacer(modifier = Modifier.height(24.dp))
            }
        }
    }
}
