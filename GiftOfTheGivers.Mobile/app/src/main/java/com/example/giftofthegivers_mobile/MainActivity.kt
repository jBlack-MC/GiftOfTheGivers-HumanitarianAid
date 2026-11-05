package com.example.giftofthegivers_mobile

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.ui.Modifier
import com.example.giftofthegivers_mobile.data.UserRole
import com.example.giftofthegivers_mobile.ui.components.BottomNavBar
import com.example.giftofthegivers_mobile.ui.screens.*
import com.example.giftofthegivers_mobile.ui.theme.GiftOfTheGiversMobileTheme
import com.example.giftofthegivers_mobile.viewmodel.MainViewModel
import com.example.giftofthegivers_mobile.viewmodel.Screen

class MainActivity : ComponentActivity() {

    private val viewModel: MainViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            GiftOfTheGiversMobileTheme {
                val currentScreen = viewModel.currentScreen

                val showBottomNav = currentScreen !is Screen.Intro && currentScreen !is Screen.Receipt

                Scaffold(
                    modifier = Modifier.fillMaxSize(),
                    bottomBar = {
                        if (showBottomNav) {
                            BottomNavBar(
                                currentScreen = currentScreen,
                                onNavigate = { screen -> viewModel.navigateTo(screen) }
                            )
                        }
                    }
                ) { innerPadding ->
                    val modifier = Modifier.padding(innerPadding)

                    when (currentScreen) {
                        is Screen.Intro -> {
                            IntroScreen(
                                onExploreClick = { viewModel.navigateTo(Screen.Home) }
                            )
                        }
                        is Screen.Home -> {
                            HomeScreen(
                                impactStats = viewModel.impactStats,
                                projects = viewModel.projects,
                                userRole = viewModel.currentUserRole,
                                onNavigate = { screen -> viewModel.navigateTo(screen) },
                                onDonateToProject = { projectId -> viewModel.prepareDonation(projectId) },
                                onSwitchRole = { role -> viewModel.setUserRole(role, if (role == UserRole.Employee) "employee@giftofthegivers.org" else "visitor@giftofthegivers.org") }
                            )
                        }
                        is Screen.Projects -> {
                            ProjectsScreen(
                                projects = viewModel.projects,
                                onDonateToProject = { projectId -> viewModel.prepareDonation(projectId) }
                            )
                        }
                        is Screen.Donate -> {
                            DonateScreen(
                                projects = viewModel.projects,
                                preselectedProjectId = viewModel.selectedProjectIdForDonation,
                                onSubmitDonation = { projId, amt, cur, name, email, consent ->
                                    viewModel.submitDonation(projId, amt, cur, name, email, consent)
                                }
                            )
                        }
                        is Screen.Receipt -> {
                            ReceiptScreen(
                                donation = viewModel.lastCreatedDonation,
                                onSimulatePayFastComplete = { donId -> viewModel.simulatePayFastComplete(donId) },
                                onBackHome = { viewModel.navigateTo(Screen.Home) }
                            )
                        }
                        is Screen.Volunteer -> {
                            VolunteerScreen(
                                onSubmitApplication = { name, email, phone, city, skills, availability, consent ->
                                    viewModel.submitVolunteer(name, email, phone, city, skills, availability, consent)
                                },
                                onSuccessSubmitted = {
                                    viewModel.navigateTo(Screen.Home)
                                }
                            )
                        }
                        is Screen.DonorPortal -> {
                            DonorPortalScreen(
                                donations = viewModel.donations,
                                onViewReceipt = { donation ->
                                    viewModel.lastCreatedDonation = donation
                                    viewModel.navigateTo(Screen.Receipt)
                                }
                            )
                        }
                        is Screen.StaffPortal -> {
                            StaffPortalScreen(
                                volunteers = viewModel.volunteers,
                                inquiries = viewModel.inquiries,
                                auditLogs = viewModel.auditLogs,
                                onApproveVolunteer = { id -> viewModel.approveVolunteer(id) },
                                onRejectVolunteer = { id -> viewModel.rejectVolunteer(id) },
                                onMarkInquiryHandled = { id -> viewModel.markInquiryHandled(id) }
                            )
                        }
                        is Screen.Privacy -> {
                            PrivacyScreen()
                        }
                    }
                }
            }
        }
    }
}
