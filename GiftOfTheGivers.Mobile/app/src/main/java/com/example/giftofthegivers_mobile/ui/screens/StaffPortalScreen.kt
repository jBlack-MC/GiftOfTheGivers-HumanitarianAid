package com.example.giftofthegivers_mobile.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.giftofthegivers_mobile.data.AuditLogEntry
import com.example.giftofthegivers_mobile.data.Inquiry
import com.example.giftofthegivers_mobile.data.VolunteerApplication
import com.example.giftofthegivers_mobile.data.VolunteerStatus

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StaffPortalScreen(
    volunteers: List<VolunteerApplication>,
    inquiries: List<Inquiry>,
    auditLogs: List<AuditLogEntry>,
    onApproveVolunteer: (String) -> Unit,
    onRejectVolunteer: (String) -> Unit,
    onMarkInquiryHandled: (String) -> Unit
) {
    var selectedTab by remember { mutableStateOf(0) }
    val tabs = listOf("Volunteers", "Inquiries", "Audit Log")

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Employee Portal", fontWeight = FontWeight.Bold) }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            ScrollableTabRow(
                selectedTabIndex = selectedTab,
                edgePadding = 16.dp
            ) {
                tabs.forEachIndexed { index, title ->
                    Tab(
                        selected = selectedTab == index,
                        onClick = { selectedTab = index },
                        text = { Text(title, fontWeight = FontWeight.SemiBold) }
                    )
                }
            }

            Spacer(modifier = Modifier.height(8.dp))

            when (selectedTab) {
                0 -> VolunteerManagementList(volunteers, onApproveVolunteer, onRejectVolunteer)
                1 -> InquiryManagementList(inquiries, onMarkInquiryHandled)
                2 -> AuditLogList(auditLogs)
            }
        }
    }
}

@Composable
fun VolunteerManagementList(
    volunteers: List<VolunteerApplication>,
    onApprove: (String) -> Unit,
    onReject: (String) -> Unit
) {
    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 16.dp)
    ) {
        items(volunteers) { vol ->
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 6.dp),
                shape = RoundedCornerShape(12.dp)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(text = vol.name, fontWeight = FontWeight.Bold, fontSize = 15.sp)
                        Text(text = vol.status.name, fontWeight = FontWeight.SemiBold, fontSize = 12.sp)
                    }
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(text = "${vol.email} · ${vol.phone}", fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    Text(text = "Skills: ${vol.skills}", fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    Text(text = "Availability: ${vol.availability}", fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)

                    if (vol.status == VolunteerStatus.Pending) {
                        Spacer(modifier = Modifier.height(12.dp))
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            Button(
                                onClick = { onApprove(vol.id) },
                                modifier = Modifier.weight(1f),
                                colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.primary)
                            ) {
                                Text("Approve")
                            }
                            OutlinedButton(
                                onClick = { onReject(vol.id) },
                                modifier = Modifier.weight(1f)
                            ) {
                                Text("Reject")
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun InquiryManagementList(
    inquiries: List<Inquiry>,
    onMarkHandled: (String) -> Unit
) {
    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 16.dp)
    ) {
        items(inquiries) { inq ->
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 6.dp),
                shape = RoundedCornerShape(12.dp)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(text = inq.subject, fontWeight = FontWeight.Bold, fontSize = 14.sp)
                        Text(
                            text = if (inq.isHandled) "Handled" else "Open",
                            color = if (inq.isHandled) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.error,
                            fontSize = 12.sp,
                            fontWeight = FontWeight.Bold
                        )
                    }
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(text = "From: ${inq.name} (${inq.email})", fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(text = inq.message, fontSize = 13.sp)

                    if (!inq.isHandled) {
                        Spacer(modifier = Modifier.height(12.dp))
                        Button(
                            onClick = { onMarkHandled(inq.id) },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("Mark as Handled")
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun AuditLogList(auditLogs: List<AuditLogEntry>) {
    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 16.dp)
    ) {
        items(auditLogs) { audit ->
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 6.dp),
                shape = RoundedCornerShape(12.dp),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
            ) {
                Column(modifier = Modifier.padding(14.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(text = audit.action, fontWeight = FontWeight.Bold, fontSize = 13.sp, color = MaterialTheme.colorScheme.primary)
                        Text(text = audit.timestamp, fontSize = 11.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(text = audit.details, fontSize = 12.sp)
                    Spacer(modifier = Modifier.height(2.dp))
                    Text(text = "Actor: ${audit.actor}", fontSize = 11.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }
        }
    }
}
